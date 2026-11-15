using Karajob.Web.Components.Settings;

namespace Karajob.Tests.Web.Components.Settings;

public class ConversionPercentTests
{
  [Theory]
  [InlineData(30, "3")]
  [InlineData(35, "3.5")]
  [InlineData(0, "0")]
  [InlineData(1000, "100")]
  public void FromPermille_Value_ReturnsPercent(int permille, string expected)
  {
    Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), ConversionPercent.FromPermille(permille));
  }

  [Theory]
  [InlineData("3", 30)]
  [InlineData("3.5", 35)]
  [InlineData("0", 0)]
  [InlineData("100", 1000)]
  public void ToPermille_ExactPercent_ReturnsPermille(string percent, int expected)
  {
    Assert.Equal(expected, ConversionPercent.ToPermille(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture)));
  }

  [Fact]
  public void ToPermille_BelowHalfStep_RoundsDown()
  {
    Assert.Equal(35, ConversionPercent.ToPermille(3.54m));
  }

  [Fact]
  public void ToPermille_ExactlyHalfStep_RoundsAwayFromZero()
  {
    Assert.Equal(36, ConversionPercent.ToPermille(3.55m));
  }

  [Fact]
  public void ToPermille_NegativeHalfStep_RoundsAwayFromZero()
  {
    Assert.Equal(-36, ConversionPercent.ToPermille(-3.55m));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(45)]
  [InlineData(999)]
  [InlineData(1000)]
  public void ToPermille_OfFromPermille_RoundTrips(int permille)
  {
    Assert.Equal(permille, ConversionPercent.ToPermille(ConversionPercent.FromPermille(permille)));
  }
}
