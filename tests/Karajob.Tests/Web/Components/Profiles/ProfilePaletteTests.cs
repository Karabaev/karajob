using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Profiles;

namespace Karajob.Tests.Web.Components.Profiles;

public class ProfilePaletteTests
{
  [Fact]
  public void Colors_HasAtLeastEightDistinctColors()
  {
    Assert.True(ProfilePalette.Colors.Count >= 8);
    Assert.Equal(ProfilePalette.Colors.Count, ProfilePalette.Colors.Distinct().Count());
  }

  [Fact]
  public void Colors_AreLowerCaseSixDigitHex()
  {
    Assert.NotEmpty(ProfilePalette.Colors);
    Assert.All(ProfilePalette.Colors, c => Assert.Matches("^#[0-9a-f]{6}$", c));
  }

  [Fact]
  public void Colors_ContainOwnerColor()
  {
    Assert.Contains(SeedData.OwnerColor, ProfilePalette.Colors);
  }
}
