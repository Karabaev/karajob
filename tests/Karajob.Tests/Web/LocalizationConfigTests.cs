using System.Net;
using System.Net.Http.Headers;
using Karajob.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

public class LocalizationConfigTests
{
  private const string Setting = "Localization:DefaultCulture";

  [Fact]
  public async Task GetPage_DefaultCultureRu_RendersRussianContent()
  {
    using var factory = WithDefaultCulture("ru");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/companies"));

    Assert.Contains("<title>Компании</title>", html);
  }

  [Fact]
  public async Task GetPage_DefaultCultureEn_RendersEnglishContent()
  {
    using var factory = WithDefaultCulture("en");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/companies"));

    Assert.Contains("<title>Companies</title>", html);
  }

  [Fact]
  public async Task GetPage_BrowserAcceptsRussian_StillRendersConfiguredEnglish()
  {
    using var factory = WithDefaultCulture("en");
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);
    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru"));

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/companies"));

    Assert.Contains("<title>Companies</title>", html);
  }

  [Fact]
  public void CreateClient_UnsupportedDefaultCulture_FailsAtStartupNamingTheSetting()
  {
    using var factory = WithDefaultCulture("de");

    var exception = Record.Exception(() => factory.CreateClient());

    Assert.NotNull(exception);
    Assert.Contains(Setting, Flatten(exception));
  }

  private static WebApplicationFactory<Program> WithDefaultCulture(string culture)
  {
    return new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting(Setting, culture));
  }

  private static string Flatten(Exception exception)
  {
    var messages = new List<string>();
    for (Exception? current = exception; current is not null; current = current.InnerException)
    {
      messages.Add(current.Message);
    }

    return string.Join(" | ", messages);
  }
}
