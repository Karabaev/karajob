using Karajob.Domain.Common;

namespace Karajob.Domain.Users;

public class User
{
  public Guid Id { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public DateTime CreatedAt { get; private set; }

  public User(string name, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    Name = name;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
