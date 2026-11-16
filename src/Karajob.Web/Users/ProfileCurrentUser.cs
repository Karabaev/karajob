using System.Security.Claims;
using Karajob.Application.Users;

namespace Karajob.Web.Users;

public class ProfileCurrentUser : ICurrentUser
{
  private Guid? _userId;

  public Guid UserId => _userId ?? throw new InvalidOperationException("No profile selected.");
  public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;

  public void SetUser(ClaimsPrincipal user)
  {
    _userId = ProfileAuthentication.GetUserId(user);
  }
}
