using Karajob.Domain.Common;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Users;

public class User
{
  public Guid Id { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public string Color { get; set => field = Normalize.HexColor(value); }
  public DateTime CreatedAt { get; private set; }

  public User(string name, string color, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    Name = name;
    Color = color;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
