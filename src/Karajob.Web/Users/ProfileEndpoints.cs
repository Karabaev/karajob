using Karajob.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Karajob.Web.Users;

public static class ProfileEndpoints
{
  public const string ProfilesPath = "/profiles";
  public const string SelectPath = "/profiles/select";
  public const string ProfileIdField = "profileId";
  public const string ReturnUrlField = "returnUrl";

  public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapPost(SelectPath, SelectAsync).AllowAnonymous();
    return endpoints;
  }

  private static async Task<IResult> SelectAsync(
    HttpContext context,
    UserService userService,
    [FromForm(Name = ProfileIdField)] string? profileId,
    [FromForm(Name = ReturnUrlField)] string? returnUrl)
  {
    if (!Guid.TryParse(profileId, out var userId))
    {
      return Results.Redirect(ProfilesPath);
    }

    var user = await userService.GetAsync(userId, context.RequestAborted);
    if (user is null)
    {
      return Results.Redirect(ProfilesPath);
    }

    await context.SignInAsync(
      ProfileAuthentication.Scheme,
      ProfileAuthentication.CreatePrincipal(user.Id, user.Name),
      new AuthenticationProperties { IsPersistent = true });
    return Results.Redirect(IsLocal(returnUrl) ? returnUrl! : "/");
  }

  private static bool IsLocal(string? url)
  {
    if (string.IsNullOrEmpty(url) || url[0] != '/')
    {
      return false;
    }

    // Browsers strip tabs and newlines from URLs, so "/\t/evil.example" would become "//evil.example".
    if (url.Any(char.IsControl)) return false;

    return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
  }
}
