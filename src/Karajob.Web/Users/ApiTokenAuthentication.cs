using System.Text.Encodings.Web;
using Karajob.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Karajob.Web.Users;

public class ApiTokenAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder)
  : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  public const string Scheme = "ApiToken";
  private const string BearerPrefix = "Bearer ";

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var header = Request.Headers[HeaderNames.Authorization].ToString();
    if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return AuthenticateResult.NoResult();
    }

    var token = header[BearerPrefix.Length..].Trim();
    var user = await Context.RequestServices.GetRequiredService<UserService>().FindByApiTokenAsync(token, Context.RequestAborted);
    if (user is null)
    {
      return AuthenticateResult.Fail("Invalid API token.");
    }

    var principal = ProfileAuthentication.CreatePrincipal(user.Id, user.Name);
    return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme));
  }

  protected override Task HandleChallengeAsync(AuthenticationProperties properties)
  {
    DisableStatusCodePages();
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.WWWAuthenticate = "Bearer";
    return Task.CompletedTask;
  }

  protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
  {
    DisableStatusCodePages();
    Response.StatusCode = StatusCodes.Status403Forbidden;
    return Task.CompletedTask;
  }

  // The site-wide status code page re-executes /not-found, which turns a bodiless 401 into an antiforgery 400 for a POST.
  private void DisableStatusCodePages()
  {
    var feature = Context.Features.Get<IStatusCodePagesFeature>();
    if (feature is not null)
    {
      feature.Enabled = false;
    }
  }
}
