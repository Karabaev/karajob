using Karajob.Domain.Common;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Users;

public class User
{
  public Guid Id { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public string Color { get; set => field = Normalize.HexColor(value); }
  public string TimeZoneId { get; set => field = UserTime.NormalizeId(value); }
  public DateTime CreatedAt { get; private set; }
  public string? ApiTokenHash { get; private set; }
  public DateTime? ApiTokenCreatedAt { get; private set; }

  public User(string name, string color, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    Name = name;
    Color = color;
    TimeZoneId = UserTime.DefaultTimeZoneId;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }

  public void IssueApiToken(string tokenHash, DateTime createdAt)
  {
    var hash = Require.NotBlank(tokenHash);
    var created = UtcGuard.Ensure(createdAt);
    ApiTokenHash = hash;
    ApiTokenCreatedAt = created;
  }

  public void RevokeApiToken()
  {
    ApiTokenHash = null;
    ApiTokenCreatedAt = null;
  }
}
