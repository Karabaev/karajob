using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Theory]
  [InlineData("/", "Доска")]
  [InlineData("/inbox", "Inbox")]
  [InlineData("/companies", "Компании")]
  [InlineData("/companies/new", "Новая компания")]
  [InlineData("/hypotheses", "Гипотезы")]
  [InlineData("/cvs", "CV")]
  public async Task GetPage_ReturnsSuccessAndPageTitle(string route, string title)
  {
    var client = factory.CreateClient();

    var response = await client.GetAsync(route);

    response.EnsureSuccessStatusCode();
    var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    Assert.Contains($"<title>{title}</title>", html);
  }

  [Fact]
  public async Task GetHome_LinksMudBlazorCss()
  {
    var client = factory.CreateClient();

    var html = await client.GetStringAsync("/");

    Assert.Matches(@"<link[^>]+href=""[^""]*MudBlazor\.min[^""]*\.css""", html);
  }

  [Fact]
  public async Task GetHome_DoesNotReferenceBootstrap()
  {
    var client = factory.CreateClient();

    var html = await client.GetStringAsync("/");

    Assert.DoesNotContain("bootstrap", html, StringComparison.OrdinalIgnoreCase);
  }
}
