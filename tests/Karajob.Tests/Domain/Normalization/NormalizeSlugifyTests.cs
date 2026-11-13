using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeSlugifyTests
{
  [Theory]
  [InlineData("Pixel Forge", "pixel-forge")]
  [InlineData("Studio 42", "studio-42")]
  [InlineData("Леста Игры", "lesta-igry")]
  [InlineData("Щука", "shchuka")]
  [InlineData("Ubisoft Montréal", "ubisoft-montreal")]
  [InlineData("Foo & Bar!", "foo-bar")]
  [InlineData("  --Multi   Space--  ", "multi-space")]
  public void Slugify_Name_ReturnsSlug(string name, string expected)
  {
    Assert.Equal(expected, Normalize.Slugify(name));
  }

  [Theory]
  [InlineData("ж", "zh")]
  [InlineData("х", "kh")]
  [InlineData("ц", "ts")]
  [InlineData("ч", "ch")]
  [InlineData("ш", "sh")]
  [InlineData("щ", "shch")]
  [InlineData("ю", "yu")]
  [InlineData("я", "ya")]
  [InlineData("й", "y")]
  [InlineData("ы", "y")]
  [InlineData("и", "i")]
  public void Slugify_CyrillicLetter_Transliterates(string letter, string expected)
  {
    Assert.Equal(expected, Normalize.Slugify(letter));
  }

  [Theory]
  [InlineData("объем", "obem")]
  [InlineData("соль", "sol")]
  [InlineData("аьъб", "ab")]
  public void Slugify_SoftAndHardSigns_AreDropped(string name, string expected)
  {
    Assert.Equal(expected, Normalize.Slugify(name));
  }

  [Theory]
  [InlineData("!!!")]
  [InlineData("   ")]
  [InlineData("")]
  public void Slugify_NoLettersOrDigits_ReturnsEmpty(string name)
  {
    Assert.Equal(string.Empty, Normalize.Slugify(name));
  }

  [Theory]
  [InlineData("Pixel Forge")]
  [InlineData("Леста Игры")]
  [InlineData("Ubisoft Montréal")]
  [InlineData("Foo & Bar!")]
  [InlineData("  --Multi   Space--  ")]
  [InlineData("a---b")]
  public void Slugify_NonEmptyResult_IsValidSlug(string name)
  {
    var slug = Normalize.Slugify(name);

    Assert.Equal(slug, Normalize.Slug(slug));
  }
}
