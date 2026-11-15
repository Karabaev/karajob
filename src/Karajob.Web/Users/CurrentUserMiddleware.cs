namespace Karajob.Web.Users;

public class CurrentUserMiddleware(RequestDelegate next)
{
  public Task InvokeAsync(HttpContext context, ProfileCurrentUser currentUser)
  {
    currentUser.SetUser(context.User);
    return next(context);
  }
}
