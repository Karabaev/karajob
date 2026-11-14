using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeLanguageCodeTests
{
  [Theory]
  [InlineData("ru", "ru")]
  [InlineData(" RU ", "ru")]
  [InlineData("En", "en")]
  [InlineData("deu", "deu")]
  public void LanguageCode_ValidInput_ReturnsTrimmedLowerCase(string input, string expected)
  {
    Assert.Equal(expected, Normalize.LanguageCode(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("r")]
  [InlineData("russ")]
  [InlineData("r1")]
  [InlineData("ру")]
  [InlineData("r u")]
  public void LanguageCode_InvalidInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.LanguageCode(input));
  }
}
