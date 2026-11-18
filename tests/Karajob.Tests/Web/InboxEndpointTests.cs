using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class InboxEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  private const string InboxPath = "/api/inbox";

  [Fact]
  public async Task Post_NoAuthorizationHeader_ReturnsUnauthorizedWithoutRedirect()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await PostAsync(client, null, NewUrl());

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Null(response.Headers.Location);
  }

  [Fact]
  public async Task Post_WrongToken_ReturnsUnauthorizedWithoutRedirectAndCreatesNothing()
  {
    var client = ProfileClient.CreateClient(factory);
    var url = NewUrl();

    var response = await PostAsync(client, "kj_" + new string('0', 64), url);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Null(response.Headers.Location);
    Assert.DoesNotContain(await InboxUrlsAsync(SeedData.OwnerId), u => u.Contains(url));
  }

  [Fact]
  public async Task Post_NonBearerScheme_ReturnsUnauthorized()
  {
    var token = await IssueTokenAsync(SeedData.OwnerId);
    var client = ProfileClient.CreateClient(factory);
    var request = new HttpRequestMessage(HttpMethod.Post, InboxPath)
    {
      Content = JsonContent(NewUrl()),
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

    var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Post_RevokedToken_ReturnsUnauthorized()
  {
    var profile = await CreateProfileAsync("Revoked");
    var token = await IssueTokenAsync(profile.Id);
    await RevokeTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);

    var response = await PostAsync(client, token, NewUrl());

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Null(response.Headers.Location);
  }

  [Fact]
  public async Task Post_OldTokenAfterRegenerate_ReturnsUnauthorizedAndNewTokenCreated()
  {
    var profile = await CreateProfileAsync("Regenerated");
    var oldToken = await IssueTokenAsync(profile.Id);
    var newToken = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);

    var oldResponse = await PostAsync(client, oldToken, NewUrl());
    var newResponse = await PostAsync(client, newToken, NewUrl());

    Assert.Equal(HttpStatusCode.Unauthorized, oldResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, newResponse.StatusCode);
  }

  [Fact]
  public async Task Post_ProfileCookieOnly_ReturnsUnauthorizedAndCreatesNothing()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);
    var url = NewUrl();

    var response = await PostAsync(client, null, url);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.DoesNotContain(await InboxUrlsAsync(SeedData.OwnerId), u => u.Contains(url));
  }

  [Fact]
  public async Task Post_ValidToken_ReturnsCreatedWithJsonBody()
  {
    var profile = await CreateProfileAsync("Writer");
    var token = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);

    var response = await PostAsync(client, token, NewUrl());

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(Guid.TryParse(body.GetProperty("vacancyId").GetString(), out var vacancyId));
    Assert.NotEqual(Guid.Empty, vacancyId);
    Assert.False(body.GetProperty("alreadyExisted").GetBoolean());
    Assert.Equal("Inbox", body.GetProperty("stage").GetString());
    Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
  }

  [Fact]
  public async Task Post_ValidToken_AddsVacancyToTokenOwnersInboxOnly()
  {
    var profile = await CreateProfileAsync("Inbox owner");
    var token = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);
    var url = NewUrl();

    var response = await PostAsync(client, token, url, "found on LinkedIn");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    var vacancyId = body.GetProperty("vacancyId").GetGuid();
    var inbox = await InboxAsync(profile.Id);
    var item = Assert.Single(inbox);
    Assert.Equal(vacancyId, item.Id);
    Assert.Contains(url, item.Url);
    Assert.Empty(await InboxUrlsAsync(SeedData.OwnerId, url));
  }

  [Fact]
  public async Task Post_ValidTokenWithoutNote_Returns201()
  {
    var profile = await CreateProfileAsync("No note");
    var token = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);
    var request = new HttpRequestMessage(HttpMethod.Post, InboxPath)
    {
      Content = new StringContent($"{{\"url\":\"{NewUrl()}\"}}", Encoding.UTF8, "application/json"),
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task Post_SameUrlTwice_ReturnsOkWithAlreadyExistedAndSameVacancyId()
  {
    var profile = await CreateProfileAsync("Duplicate");
    var token = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);
    var url = NewUrl();

    var first = await PostAsync(client, token, url);
    var second = await PostAsync(client, token, url);

    Assert.Equal(HttpStatusCode.Created, first.StatusCode);
    Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
    var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(secondBody.GetProperty("alreadyExisted").GetBoolean());
    Assert.Equal(firstBody.GetProperty("vacancyId").GetGuid(), secondBody.GetProperty("vacancyId").GetGuid());
    Assert.Single(await InboxAsync(profile.Id));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not a url")]
  [InlineData("ftp://example.com/job")]
  public async Task Post_InvalidUrl_ReturnsBadRequestWithErrorsAndCreatesNothing(string url)
  {
    var profile = await CreateProfileAsync("Invalid url");
    var token = await IssueTokenAsync(profile.Id);
    var client = ProfileClient.CreateClient(factory);

    var response = await PostAsync(client, token, url);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    var errors = body.GetProperty("errors");
    Assert.Equal(JsonValueKind.Object, errors.ValueKind);
    Assert.NotEmpty(errors.EnumerateObject());
    Assert.Empty(await InboxAsync(profile.Id));
  }

  [Fact]
  public async Task Post_TwoProfilesWithOwnTokens_WriteOnlyIntoTheirOwnInbox()
  {
    var first = await CreateProfileAsync("First");
    var second = await CreateProfileAsync("Second");
    var firstToken = await IssueTokenAsync(first.Id);
    var secondToken = await IssueTokenAsync(second.Id);
    var client = ProfileClient.CreateClient(factory);
    var firstUrl = NewUrl();
    var secondUrl = NewUrl();

    var firstResponse = await PostAsync(client, firstToken, firstUrl);
    var secondResponse = await PostAsync(client, secondToken, secondUrl);

    Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    var firstInbox = Assert.Single(await InboxAsync(first.Id));
    var secondInbox = Assert.Single(await InboxAsync(second.Id));
    Assert.Contains(firstUrl, firstInbox.Url);
    Assert.Contains(secondUrl, secondInbox.Url);
  }

  private static string NewUrl()
  {
    return $"https://jobs.example/api-inbox/{Guid.NewGuid():N}";
  }

  private static StringContent JsonContent(string url, string? note = null)
  {
    return new StringContent(JsonSerializer.Serialize(new { url, note }), Encoding.UTF8, "application/json");
  }

  private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string? token, string url, string? note = null)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, InboxPath) { Content = JsonContent(url, note) };
    if (token is not null)
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    return await client.SendAsync(request);
  }

  private async Task<UserDto> CreateProfileAsync(string name)
  {
    using var scope = factory.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<UserService>().CreateAsync(new UserInput(name, "#26a69a"));
  }

  private async Task<string> IssueTokenAsync(Guid userId)
  {
    using var scope = factory.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<UserService>().IssueApiTokenAsync(userId);
  }

  private async Task RevokeTokenAsync(Guid userId)
  {
    using var scope = factory.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<UserService>().RevokeApiTokenAsync(userId);
  }

  private async Task<IReadOnlyList<VacancyListItem>> InboxAsync(Guid userId)
  {
    using var scope = factory.Services.CreateScope();
    var currentUser = Assert.IsType<ProfileCurrentUser>(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    currentUser.SetUser(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test")));
    return await scope.ServiceProvider.GetRequiredService<VacancyService>().ListAsync(VacancyStage.Inbox);
  }

  private async Task<IReadOnlyList<string>> InboxUrlsAsync(Guid userId, string? onlyContaining = null)
  {
    var urls = (await InboxAsync(userId)).Select(v => v.Url);
    return onlyContaining is null ? urls.ToList() : urls.Where(u => u.Contains(onlyContaining)).ToList();
  }
}
