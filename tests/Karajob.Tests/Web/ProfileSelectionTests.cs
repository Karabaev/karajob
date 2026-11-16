using System.Net;
using System.Security.Claims;
using AngleSharp.Html.Parser;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class ProfileSelectionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Fact]
  public async Task GetInbox_WithoutProfileCookie_RedirectsToProfilesWithReturnUrl()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync("/inbox");

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal("/profiles?returnUrl=/inbox", ProfileClient.LocationOf(response));
  }

  [Fact]
  public async Task GetProfiles_WithoutProfileCookie_ReturnsProfilesPageWithTitle()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync("/profiles");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    Assert.Contains("<title>Profiles</title>", html);
    Assert.Contains("Owner", html);
  }

  [Fact]
  public async Task GetProfiles_WithoutProfileCookie_ContainsSelectFormForOwner()
  {
    var client = ProfileClient.CreateClient(factory);

    var html = await client.GetStringAsync("/profiles");

    var document = new HtmlParser().ParseDocument(html);
    var ownerInput = document
      .QuerySelectorAll("form[action='/profiles/select'] input[name='profileId']")
      .SingleOrDefault(i => i.GetAttribute("value") == SeedData.OwnerId.ToString());
    Assert.NotNull(ownerInput);
    Assert.Equal("hidden", ownerInput.GetAttribute("type"));
  }

  [Theory]
  [InlineData("/hypotheses", "/hypotheses")]
  [InlineData("/inbox", "/inbox")]
  [InlineData("https://evil.example/", "/")]
  [InlineData("//evil.example/x", "/")]
  [InlineData("/\\evil.example", "/")]
  [InlineData("/\t/evil.example", "/")]
  [InlineData("/\n/evil.example", "/")]
  public async Task PostSelect_OwnerWithReturnUrl_RedirectsToSafeLocalUrl(string returnUrl, string expected)
  {
    var client = ProfileClient.CreateClient(factory);
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await ProfileClient.SelectAsync(client, token, SeedData.OwnerId.ToString(), returnUrl);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal(expected, ProfileClient.LocationOf(response));
    Assert.True(ProfileClient.SetsProfileCookie(response));
  }

  [Fact]
  public async Task PostSelect_WithoutAntiforgeryToken_ReturnsBadRequestWithoutCookie()
  {
    var client = ProfileClient.CreateClient(factory);
    await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await ProfileClient.SelectAsync(client, string.Empty, SeedData.OwnerId.ToString(), "/");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.False(ProfileClient.SetsProfileCookie(response));
  }

  [Fact]
  public async Task GetUnknownPage_WithoutProfileCookie_ReturnsNotFoundPage()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync("/does-not-exist");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    Assert.Contains("<title>Not found</title>", html);
  }

  [Fact]
  public void Host_RegistersCurrentUserCircuitHandler()
  {
    using var scope = factory.Services.CreateScope();

    var handlers = scope.ServiceProvider.GetServices<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>();

    Assert.Contains(handlers, h => h is CurrentUserCircuitHandler);
  }

  [Fact]
  public async Task PostSelect_UnknownProfile_RedirectsToProfilesWithoutCookie()
  {
    var client = ProfileClient.CreateClient(factory);
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await ProfileClient.SelectAsync(client, token, Guid.NewGuid().ToString(), "/hypotheses");

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal("/profiles", ProfileClient.LocationOf(response).Split('?')[0]);
    Assert.False(ProfileClient.SetsProfileCookie(response));
  }

  [Fact]
  public async Task PostSelect_NotAGuidProfileId_RedirectsToProfilesWithoutCookie()
  {
    var client = ProfileClient.CreateClient(factory);
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await ProfileClient.SelectAsync(client, token, "not-a-guid", "/");

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.StartsWith("/profiles", ProfileClient.LocationOf(response));
    Assert.False(ProfileClient.SetsProfileCookie(response));
  }

  [Fact]
  public async Task GetInbox_AfterSelectingOwner_ReturnsInboxPage()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync("/inbox");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    Assert.Contains("<title>Inbox</title>", html);
  }

  [Fact]
  public async Task GetInbox_CookieOfDeletedProfile_RedirectsToProfilesWithReturnUrl()
  {
    var profile = await CreateProfileAsync("Temp");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, profile.Id);
    await DeleteProfileAsync(profile.Id);

    var response = await client.GetAsync("/inbox");

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal("/profiles?returnUrl=/inbox", ProfileClient.LocationOf(response));
  }

  [Fact]
  public async Task GetInbox_OwnerProfile_ShowsSeededVacancies()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var html = await client.GetStringAsync("/inbox");

    Assert.Contains("jobs.example", html);
  }

  [Fact]
  public async Task GetInbox_NewProfile_DoesNotShowOwnersVacancies()
  {
    var profile = await CreateProfileAsync("Fresh");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, profile.Id);

    var response = await client.GetAsync("/inbox");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.DoesNotContain("jobs.example", await response.Content.ReadAsStringAsync());
  }

  [Fact]
  public async Task GetVersionFile_WithoutProfileCookie_RedirectsToProfiles()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync($"/cvs/versions/{Guid.NewGuid()}/file");

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.StartsWith("/profiles", ProfileClient.LocationOf(response));
  }

  private async Task<UserDto> CreateProfileAsync(string name)
  {
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<UserService>();
    return await service.CreateAsync(new UserInput(name, "#26a69a"));
  }

  private async Task DeleteProfileAsync(Guid id)
  {
    using var scope = factory.Services.CreateScope();
    var currentUser = Assert.IsType<ProfileCurrentUser>(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    currentUser.SetUser(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test")));
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var user = await context.Users.SingleAsync(u => u.Id == id);
    context.SearchGoals.Remove(await context.SearchGoals.SingleAsync());
    context.Users.Remove(user);
    await context.SaveChangesAsync();
  }
}
