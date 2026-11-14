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

  [Fact]
  public void Ctor_UpperCaseColor_StoresLowerCase()
  {
    var user = new User("Anna", "#26A69A", Now);

    Assert.Equal("#26a69a", user.Color);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("red")]
  [InlineData("#abc")]
  [InlineData("#gggggg")]
  public void Ctor_InvalidColor_ThrowsArgumentException(string color)
  {
    Assert.ThrowsAny<ArgumentException>(() => new User("Anna", color, Now));
  }

  [Fact]
  public void SetColor_UpperCase_StoresLowerCase()
  {
    var user = new User("Anna", "#26a69a", Now);

    user.Color = "#7E57C2";

    Assert.Equal("#7e57c2", user.Color);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("7e57c2")]
  [InlineData("#7e57c2ff")]
  public void SetColor_Invalid_ThrowsArgumentExceptionAndKeepsOldColor(string color)
  {
    var user = new User("Anna", "#26a69a", Now);

    Assert.ThrowsAny<ArgumentException>(() => user.Color = color);
    Assert.Equal("#26a69a", user.Color);
  }
}
