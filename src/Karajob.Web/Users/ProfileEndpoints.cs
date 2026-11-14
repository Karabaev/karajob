namespace Karajob.Web.Users;

public static class ProfileEndpoints
{
  public const string ProfilesPath = "/profiles";
  public const string SelectPath = "/profiles/select";
  public const string ProfileIdField = "profileId";
  public const string ReturnUrlField = "returnUrl";

  public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
  {
    return endpoints;
  }
}
