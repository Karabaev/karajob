using Karajob.Domain.Users;

namespace Karajob.Tests.Domain.Users;

public class UserTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndTrimsName()
  {
    var user = new User("  Anna ", Now);

    Assert.NotEqual(Guid.Empty, user.Id);
    Assert.Equal("Anna", user.Name);
    Assert.Equal(Now, user.CreatedAt);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => new User(name, Now));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() => new User("Anna", new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }
}
