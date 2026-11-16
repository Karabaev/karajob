using Karajob.Application.Users;

namespace Karajob.Infrastructure.Users;

public class FixedCurrentUser(Guid userId, TimeZoneInfo? timeZone = null) : ICurrentUser
{
  public Guid UserId { get; } = userId;
  public TimeZoneInfo TimeZone { get; } = timeZone ?? TimeZoneInfo.Utc;
}
