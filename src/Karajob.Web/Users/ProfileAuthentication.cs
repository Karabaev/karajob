using System.Security.Claims;
using Karajob.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karajob.Web.Users;

public static class ProfileAuthentication
{
  public const string Scheme = "Profile";
  public const string CookieName = "karajob.profile";

  public static IServiceCollection AddProfileAuthentication(this IServiceCollection services)
  {
    services.AddAuthentication(Scheme)
      .AddCookie(Scheme, options =>
      {
        options.Cookie.Name = CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        options.LoginPath = ProfileEndpoints.ProfilesPath;
        options.ReturnUrlParameter = ProfileEndpoints.ReturnUrlField;
        options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
        options.Events.OnRedirectToLogin = RedirectToProfiles;
        options.Events.OnRedirectToAccessDenied = RedirectToProfiles;
      });
    services.AddAuthorization();
    services.AddCascadingAuthenticationState();
    services.AddScoped<ProfileCurrentUser>();
    services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ProfileCurrentUser>());
    services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CurrentUserCircuitHandler>());
    return services;
  }

  public static ClaimsPrincipal CreatePrincipal(Guid userId, string name)
  {
    var identity = new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")), new Claim(ClaimTypes.Name, name)],
      Scheme);
    return new ClaimsPrincipal(identity);
  }

  public static Guid? GetUserId(ClaimsPrincipal user)
  {
    if (user.Identity?.IsAuthenticated != true)
    {
      return null;
    }

    return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
  }

  // The default handler builds an absolute Location from Request.Host, which is wrong behind a proxy that rewrites Host.
  private static Task RedirectToProfiles(RedirectContext<CookieAuthenticationOptions> context)
  {
    var request = context.Request;
    var returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
    context.Response.StatusCode = StatusCodes.Status302Found;
    context.Response.Headers.Location = QueryHelpers.AddQueryString(
      $"{request.PathBase}{ProfileEndpoints.ProfilesPath}",
      ProfileEndpoints.ReturnUrlField,
      returnUrl);
    return Task.CompletedTask;
  }

  private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
  {
    var userId = GetUserId(context.Principal!);
    var users = context.HttpContext.RequestServices.GetRequiredService<UserService>();
    if (userId is null || !await users.ExistsAsync(userId.Value, context.HttpContext.RequestAborted))
    {
      context.RejectPrincipal();
      await context.HttpContext.SignOutAsync(Scheme);
    }
  }
}
