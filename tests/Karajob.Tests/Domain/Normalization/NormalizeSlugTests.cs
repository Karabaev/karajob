using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeSlugTests
{
  [Theory]
  [InlineData(" Ubisoft ", "ubisoft")]
  [InlineData("Blizzard-Entertainment", "blizzard-entertainment")]
  [InlineData("4a-games", "4a-games")]
  public void Slug_ValidInput_ReturnsNormalized(string input, string expected)
  {
    Assert.Equal(expected, Normalize.Slug(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("ubi soft")]
  [InlineData("-ubisoft")]
  [InlineData("ubisoft-")]
  [InlineData("ubi--soft")]
  [InlineData("ubisoft!")]
  [InlineData("юбисофт")]
  [InlineData("ubi_soft")]
  public void Slug_InvalidInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.Slug(input));
  }
}
