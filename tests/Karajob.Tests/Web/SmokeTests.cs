using System.Net;
using Karajob.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Theory]
  [InlineData("/", "Board")]
  [InlineData("/inbox", "Inbox")]
  [InlineData("/companies", "Companies")]
  [InlineData("/companies/new", "New company")]
  [InlineData("/tracks", "Tracks")]
  [InlineData("/cvs", "CV")]
  public async Task GetPage_ReturnsSuccessAndPageTitle(string route, string title)
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync(route);

    response.EnsureSuccessStatusCode();
    var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    Assert.Contains($"<title>Karajob | {title}</title>", html);
  }

  [Fact]
  public async Task GetHome_LinksMudBlazorCss()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var html = await client.GetStringAsync("/");

    Assert.Matches(@"<link[^>]+href=""[^""]*MudBlazor\.min[^""]*\.css""", html);
  }

  [Fact]
  public async Task GetHome_DoesNotReferenceBootstrap()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var html = await client.GetStringAsync("/");

    Assert.DoesNotContain("bootstrap", html, StringComparison.OrdinalIgnoreCase);
  }
}
