namespace Karajob.Domain.Users;

public class User
{
  public Guid Id { get; private set; }
  public string Name { get; set; }
  public DateTime CreatedAt { get; private set; }

  public User(string name, DateTime createdAt) => throw new NotImplementedException();
}
