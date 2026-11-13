using Karajob.Application.Users;

namespace Karajob.Infrastructure.Users;

public class FixedCurrentUser(Guid userId) : ICurrentUser
{
  public Guid UserId { get; } = userId;
}
