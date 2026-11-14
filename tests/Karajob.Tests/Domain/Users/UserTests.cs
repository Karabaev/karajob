using Karajob.Domain.Users;

namespace Karajob.Tests.Domain.Users;

public class UserTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndTrimsName()
  {
    var user = new User("  Anna ", "#26a69a", Now);

    Assert.NotEqual(Guid.Empty, user.Id);
    Assert.Equal("Anna", user.Name);
    Assert.Equal(Now, user.CreatedAt);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => new User(name, "#26a69a", Now));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() => new User("Anna", "#26a69a", new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }
}
