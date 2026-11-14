using System.Security.Claims;
using Karajob.Application.Users;

namespace Karajob.Web.Users;

public class ProfileCurrentUser : ICurrentUser
{
  public Guid UserId => throw new NotImplementedException();

  public void SetUser(ClaimsPrincipal user)
  {
    throw new NotImplementedException();
  }
}
