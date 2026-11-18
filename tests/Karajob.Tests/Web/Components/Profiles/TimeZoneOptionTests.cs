using Karajob.Web.Components.Profiles;

namespace Karajob.Tests.Web.Components.Profiles;

public class TimeZoneOptionTests
{
  private static readonly IReadOnlyList<TimeZoneOption> Zones =
  [
    new("UTC", "(UTC+00:00) UTC"),
    new("Europe/Belgrade", "(UTC+01:00) Europe/Belgrade"),
    new("Asia/Tokyo", "(UTC+09:00) Asia/Tokyo")
  ];

  [Fact]
  public void Search_BlankText_ReturnsEveryZone()
  {
    Assert.Equal(["UTC", "Europe/Belgrade", "Asia/Tokyo"], TimeZoneOption.Search(Zones, "  "));
  }

  [Fact]
  public void Search_TextIsLabelOfSelectedZone_ReturnsEveryZone()
  {
    Assert.Equal(["UTC", "Europe/Belgrade", "Asia/Tokyo"], TimeZoneOption.Search(Zones, "(UTC+00:00) UTC"));
  }

  [Fact]
  public void Search_PartialText_ReturnsMatchingZonesIgnoringCase()
  {
    Assert.Equal(["Europe/Belgrade"], TimeZoneOption.Search(Zones, "belgr"));
  }

  [Fact]
  public void Search_UnknownText_ReturnsNothing()
  {
    Assert.Empty(TimeZoneOption.Search(Zones, "Mars"));
  }
}
