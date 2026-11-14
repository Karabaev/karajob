using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeHexColorTests
{
  [Theory]
  [InlineData("#7e57c2", "#7e57c2")]
  [InlineData("#7E57C2", "#7e57c2")]
  [InlineData("  #26A69A  ", "#26a69a")]
  [InlineData("#000000", "#000000")]
  [InlineData("#FFFFFF", "#ffffff")]
  public void HexColor_ValidInput_ReturnsTrimmedLowerCase(string input, string expected)
  {
    Assert.Equal(expected, Normalize.HexColor(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("red")]
  [InlineData("#abc")]
  [InlineData("#gggggg")]
  [InlineData("7e57c2")]
  [InlineData("#7e57c2ff")]
  public void HexColor_InvalidInput_ThrowsArgumentException(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.HexColor(input));
  }
}
