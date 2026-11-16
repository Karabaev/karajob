using System.Net;
using System.Security.Claims;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class RelativeRedirectTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  private const string ForeignHost = "example.invalid";

  [Theory]
  [InlineData("example.invalid", "/inbox")]
  [InlineData("example.invalid:8085", "/inbox")]
  [InlineData("example.invalid", "/")]
  [InlineData("example.invalid:8085", "/")]
  public async Task Get_ProtectedPathWithoutCookie_RedirectsToRelativeLocation(string host, string path)
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await SendAsync(client, HttpMethod.Get, path, host);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    AssertRelative(RawLocationOf(response));
  }

  [Fact]
  public async Task Get_ProtectedPathWithQueryString_RedirectsToProfilesWithEscapedReturnUrl()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await SendAsync(client, HttpMethod.Get, "/hypotheses?a=1&b=2", ForeignHost);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal(
      "/profiles?returnUrl=" + Uri.EscapeDataString("/hypotheses?a=1&b=2"),
      RawLocationOf(response));
  }

  [Fact]
  public async Task PostSelect_WithReturnUrlFromRedirect_RedirectsBackToOriginalPathAndQuery()
  {
    var client = ProfileClient.CreateClient(factory);
    var redirect = await SendAsync(client, HttpMethod.Get, "/hypotheses?a=1&b=2", ForeignHost);
    var location = RawLocationOf(redirect);
    Assert.StartsWith("/profiles?returnUrl=", location);
    var returnUrl = Uri.UnescapeDataString(location[(location.IndexOf("returnUrl=", StringComparison.Ordinal) + "returnUrl=".Length)..]);
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await SelectWithHostAsync(client, token, SeedData.OwnerId.ToString(), returnUrl, ForeignHost);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal("/hypotheses?a=1&b=2", RawLocationOf(response));
  }

  [Fact]
  public async Task GetInbox_CookieOfDeletedProfile_RedirectsToRelativeLocation()
  {
    var profile = await CreateProfileAsync("Temp");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, profile.Id);
    await DeleteProfileAsync(profile.Id);

    var response = await SendAsync(client, HttpMethod.Get, "/inbox", ForeignHost);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    AssertRelative(RawLocationOf(response));
  }

  [Fact]
  public async Task PostSelect_UnknownProfile_RedirectsToRelativeLocation()
  {
    var client = ProfileClient.CreateClient(factory);
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await SelectWithHostAsync(client, token, Guid.NewGuid().ToString(), "/hypotheses", ForeignHost);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    AssertRelative(RawLocationOf(response));
  }

  [Fact]
  public async Task Get_ProtectedPathUnderPathBase_KeepsPathBaseInLocationAndReturnUrl()
  {
    var client = factory
      .WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddTransient<IStartupFilter, PathBaseStartupFilter>()))
      .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var response = await SendAsync(client, HttpMethod.Get, "/inbox", ForeignHost);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.Equal("/kj/profiles?returnUrl=" + Uri.EscapeDataString("/kj/inbox"), RawLocationOf(response));
  }

  private class PathBaseStartupFilter : IStartupFilter
  {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
      return app =>
      {
        app.Use((context, nextMiddleware) =>
        {
          context.Request.PathBase = "/kj";
          return nextMiddleware(context);
        });
        next(app);
      };
    }
  }

  private static void AssertRelative(string location)
  {
    Assert.StartsWith("/", location);
    Assert.False(location.StartsWith("//", StringComparison.Ordinal), $"Location '{location}' is protocol-relative.");
    Assert.DoesNotContain(ForeignHost, location);
    Assert.True(Uri.IsWellFormedUriString(location, UriKind.Relative), $"Location '{location}' is not a well-formed relative URI.");
  }

  private static string RawLocationOf(HttpResponseMessage response)
  {
    var location = response.Headers.Location;
    Assert.NotNull(location);
    return location.OriginalString;
  }

  private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string host)
  {
    var request = new HttpRequestMessage(method, path);
    request.Headers.Host = host;
    return await client.SendAsync(request);
  }

  private static async Task<HttpResponseMessage> SelectWithHostAsync(
    HttpClient client, string token, string profileId, string returnUrl, string host)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, ProfileEndpoints.SelectPath)
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        [ProfileEndpoints.ProfileIdField] = profileId,
        [ProfileEndpoints.ReturnUrlField] = returnUrl,
        ["__RequestVerificationToken"] = token,
      }),
    };
    request.Headers.Host = host;
    return await client.SendAsync(request);
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
