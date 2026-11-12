using Karajob.Domain.Common;

namespace Karajob.Tests.Domain.Common;

public class RequireTests
{
  [Fact]
  public void NotEmpty_EmptyGuid_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => Require.NotEmpty(Guid.Empty));
  }

  [Fact]
  public void NotEmpty_NonEmptyGuid_ReturnsValue()
  {
    var id = Guid.NewGuid();

    Assert.Equal(id, Require.NotEmpty(id));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void NotBlank_BlankValue_Throws(string value)
  {
    Assert.ThrowsAny<ArgumentException>(() => Require.NotBlank(value));
  }

  [Fact]
  public void NotBlank_PaddedValue_ReturnsTrimmed()
  {
    Assert.Equal("x", Require.NotBlank(" x "));
  }
}
