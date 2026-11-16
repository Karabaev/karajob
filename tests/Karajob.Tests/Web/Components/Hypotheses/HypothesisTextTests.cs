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
}
