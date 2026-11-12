using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeAliasTests
{
  [Theory]
  [InlineData("  Ubisoft   Entertainment ", "ubisoft entertainment")]
  [InlineData("UBISOFT", "ubisoft")]
  public void Alias_ValidInput_ReturnsNormalized(string input, string expected)
  {
    Assert.Equal(expected, Normalize.Alias(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Alias_BlankInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.Alias(input));
  }
}
