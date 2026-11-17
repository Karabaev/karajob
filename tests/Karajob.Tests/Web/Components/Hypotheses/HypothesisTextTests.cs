using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Web.Components.Hypotheses;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisTextTests
{
  [Theory]
  [InlineData(HypothesisStatus.Draft, "Draft")]
  [InlineData(HypothesisStatus.Active, "Active")]
  [InlineData(HypothesisStatus.Expanded, "Expanded")]
  [InlineData(HypothesisStatus.Paused, "Paused")]
  [InlineData(HypothesisStatus.Disabled, "Disabled")]
  public void Status_ReturnsEnglishLabel(HypothesisStatus status, string expected)
  {
    Assert.Equal(expected, HypothesisText.Status(status));
  }

  [Theory]
  [InlineData(WorkFormat.Remote, "Remote")]
  [InlineData(WorkFormat.Hybrid, "Hybrid")]
  [InlineData(WorkFormat.OnSite, "Office")]
  [InlineData(WorkFormat.Relocation, "Relocation")]
  [InlineData(WorkFormat.Remote | WorkFormat.Relocation, "Remote, Relocation")]
  public void Format_ReturnsEnglishLabels(WorkFormat format, string expected)
  {
    Assert.Equal(expected, HypothesisText.Format(format));
  }

  [Fact]
  public void Summary_FullItem_JoinsLanguagePlatformsAndFormat()
  {
    var item = Item("en", WorkFormat.Remote, "PC", "Mobile");

    Assert.Equal("EN · PC, Mobile · Remote", HypothesisText.Summary(item));
  }

  [Fact]
  public void Summary_NoPlatforms_SkipsPlatformPart()
  {
    var item = Item("ru", WorkFormat.Hybrid);

    Assert.Equal("RU · Hybrid", HypothesisText.Summary(item));
  }

  [Fact]
  public void Summary_SeveralPlatforms_JoinsThemWithComma()
  {
    var item = Item("en", WorkFormat.OnSite, "PC", "VR", "Web");

    Assert.Equal("EN · PC, VR, Web · Office", HypothesisText.Summary(item));
  }

  [Fact]
  public void Summary_MultiFlagWorkFormat_UsesFormatOutput()
  {
    var format = WorkFormat.Remote | WorkFormat.Hybrid;
    var item = Item("en", format, "PC");

    Assert.Equal($"EN · PC · {HypothesisText.Format(format)}", HypothesisText.Summary(item));
  }

  private static HypothesisListItem Item(string languageCode, WorkFormat format, params string[] platforms)
  {
    return new HypothesisListItem(Guid.NewGuid(), "Name", Guid.NewGuid(), "Role", format, languageCode, HypothesisStatus.Active, platforms);
  }
}
