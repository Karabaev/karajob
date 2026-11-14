using System.Net;
using AngleSharp.Html.Parser;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

internal static class ProfileClient
{
  public const string CookieName = "karajob.profile";

  public static HttpClient CreateClient(WebApplicationFactory<Program> factory)
  {
    return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
  }

  public static async Task<string> FetchAntiforgeryTokenAsync(HttpClient client)
  {
    var response = await client.GetAsync(ProfileEndpoints.ProfilesPath);
    Assert.True(
      response.StatusCode == HttpStatusCode.OK,
      $"GET {ProfileEndpoints.ProfilesPath} returned {(int)response.StatusCode} instead of 200.");
    var html = await response.Content.ReadAsStringAsync();
    var token = new HtmlParser().ParseDocument(html)
      .QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
    Assert.False(string.IsNullOrEmpty(token), $"No __RequestVerificationToken hidden input found on {ProfileEndpoints.ProfilesPath}.");
    return token;
  }

  public static async Task<HttpResponseMessage> SelectAsync(HttpClient client, string token, string profileId, string returnUrl)
  {
    var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
      [ProfileEndpoints.ProfileIdField] = profileId,
      [ProfileEndpoints.ReturnUrlField] = returnUrl,
      ["__RequestVerificationToken"] = token,
    });
    return await client.PostAsync(ProfileEndpoints.SelectPath, form);
  }

  public static async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program> factory, Guid userId)
  {
    var client = CreateClient(factory);
    var token = await FetchAntiforgeryTokenAsync(client);

    var response = await SelectAsync(client, token, userId.ToString(), "/");

    Assert.True(
      response.StatusCode == HttpStatusCode.Found,
      $"POST {ProfileEndpoints.SelectPath} for profile {userId} returned {(int)response.StatusCode} instead of 302.");
    Assert.True(SetsProfileCookie(response), $"POST {ProfileEndpoints.SelectPath} did not set the '{CookieName}' cookie.");
    return client;
  }

  public static bool SetsProfileCookie(HttpResponseMessage response)
  {
    return response.Headers.TryGetValues("Set-Cookie", out var values)
      && values.Any(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal));
  }

  public static string LocationOf(HttpResponseMessage response)
  {
    var location = response.Headers.Location;
    Assert.NotNull(location);
    return Uri.UnescapeDataString(location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString);
  }
}
