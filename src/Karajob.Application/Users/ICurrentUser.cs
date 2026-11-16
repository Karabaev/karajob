namespace Karajob.Application.Users;

public interface ICurrentUser
{
  Guid UserId { get; }
  TimeZoneInfo TimeZone { get; }
}
