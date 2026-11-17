using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Web.Components.Tracks;

namespace Karajob.Tests.Web.Components.Tracks;

public class TrackTextTests
{
  [Theory]
  [InlineData(TrackStatus.Draft, "Draft")]
  [InlineData(TrackStatus.Active, "Active")]
  [InlineData(TrackStatus.Expanded, "Expanded")]
  [InlineData(TrackStatus.Paused, "Paused")]
  [InlineData(TrackStatus.Disabled, "Disabled")]
  public void Status_ReturnsEnglishLabel(TrackStatus status, string expected)
  {
    Assert.Equal(expected, TrackText.Status(status));
  }

  [Theory]
  [InlineData(WorkFormat.Remote, "Remote")]
  [InlineData(WorkFormat.Hybrid, "Hybrid")]
  [InlineData(WorkFormat.OnSite, "Office")]
  [InlineData(WorkFormat.Relocation, "Relocation")]
  [InlineData(WorkFormat.Remote | WorkFormat.Relocation, "Remote, Relocation")]
  public void Format_ReturnsEnglishLabels(WorkFormat format, string expected)
  {
    Assert.Equal(expected, TrackText.Format(format));
  }

  [Fact]
  public void Summary_FullItem_JoinsLanguagePlatformsAndFormat()
  {
    var item = Item("en", WorkFormat.Remote, "PC", "Mobile");

    Assert.Equal("EN · PC, Mobile · Remote", TrackText.Summary(item));
  }

  [Fact]
  public void Summary_NoPlatforms_SkipsPlatformPart()
  {
    var item = Item("ru", WorkFormat.Hybrid);

    Assert.Equal("RU · Hybrid", TrackText.Summary(item));
  }

  [Fact]
  public void Summary_SeveralPlatforms_JoinsThemWithComma()
  {
    var item = Item("en", WorkFormat.OnSite, "PC", "VR", "Web");

    Assert.Equal("EN · PC, VR, Web · Office", TrackText.Summary(item));
  }

  [Fact]
  public void Summary_MultiFlagWorkFormat_UsesFormatOutput()
  {
    var format = WorkFormat.Remote | WorkFormat.Hybrid;
    var item = Item("en", format, "PC");

    Assert.Equal($"EN · PC · {TrackText.Format(format)}", TrackText.Summary(item));
  }

  private static TrackListItem Item(string languageCode, WorkFormat format, params string[] platforms)
  {
    return new TrackListItem(Guid.NewGuid(), "Name", Guid.NewGuid(), "Role", format, languageCode, TrackStatus.Active, platforms);
  }
}
