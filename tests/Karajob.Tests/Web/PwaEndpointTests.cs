using System.Net;
using Karajob.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

public class PwaEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  private const string SharePath = "/quick-add?url=https%3A%2F%2Fa.com%2Fj&text=hi";
  private const string ProfilesPrefix = "/profiles?returnUrl=";

  [Fact]
  public async Task GetQuickAdd_WithoutProfileCookie_RedirectsToProfilesKeepingQueryInReturnUrl()
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync(SharePath);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    var location = ProfileClient.LocationOf(response);
    Assert.StartsWith(ProfilesPrefix, location);
    Assert.Equal(SharePath, location[ProfilesPrefix.Length..]);
  }

  [Fact]
  public async Task PostSelect_WithQuickAddReturnUrl_RedirectsBackToQuickAddWithQuery()
  {
    var client = ProfileClient.CreateClient(factory);
    var redirect = await client.GetAsync(SharePath);
    var returnUrl = ProfileClient.LocationOf(redirect)[ProfilesPrefix.Length..];
    var token = await ProfileClient.FetchAntiforgeryTokenAsync(client);

    var response = await ProfileClient.SelectAsync(client, token, SeedData.OwnerId.ToString(), returnUrl);

    Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    Assert.NotNull(response.Headers.Location);
    Assert.Equal(SharePath, response.Headers.Location.OriginalString);
  }

  [Theory]
  [InlineData("/manifest.webmanifest")]
  [InlineData("/service-worker.js")]
  public async Task GetStaticPwaFile_WithoutProfileCookie_ReturnsOk(string path)
  {
    var client = ProfileClient.CreateClient(factory);

    var response = await client.GetAsync(path);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetProfiles_WithoutProfileCookie_LinksManifest()
  {
    var client = ProfileClient.CreateClient(factory);

    var html = await client.GetStringAsync("/profiles");

    Assert.Contains("rel=\"manifest\"", html);
  }
}
