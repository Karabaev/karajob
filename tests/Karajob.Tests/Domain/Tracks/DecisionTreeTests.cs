using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class DecisionTreeTests
{
  private static DecisionTreeInput Input(
    int marketVacancies = 20,
    int marketThreshold = 10,
    int positive = 5,
    int matured = 100,
    int thresholdPermille = 30,
    int minSampleSize = 30,
    bool wellBeingLow = false)
  {
    return new DecisionTreeInput(marketVacancies, marketThreshold, positive, matured, thresholdPermille, minSampleSize, wellBeingLow);
  }

  [Fact]
  public void Suggest_WellBeingLow_ReturnsDisable()
  {
    Assert.Equal(DecisionHint.Disable, DecisionTree.Suggest(Input(wellBeingLow: true)));
  }

  [Fact]
  public void Suggest_WellBeingLowAndNarrowMarket_ReturnsDisable()
  {
    Assert.Equal(DecisionHint.Disable, DecisionTree.Suggest(Input(marketVacancies: 0, wellBeingLow: true)));
  }

  [Fact]
  public void Suggest_WellBeingLowAndInsufficientData_ReturnsDisable()
  {
    Assert.Equal(DecisionHint.Disable, DecisionTree.Suggest(Input(matured: 0, positive: 0, wellBeingLow: true)));
  }

  [Fact]
  public void Suggest_WellBeingLowAndGoodConversion_ReturnsDisable()
  {
    Assert.Equal(DecisionHint.Disable, DecisionTree.Suggest(Input(positive: 50, matured: 100, wellBeingLow: true)));
  }

  [Fact]
  public void Suggest_MarketBelowThreshold_ReturnsExpand()
  {
    Assert.Equal(DecisionHint.Expand, DecisionTree.Suggest(Input(marketVacancies: 9, marketThreshold: 10)));
  }

  [Fact]
  public void Suggest_MarketBelowThresholdWithInsufficientData_ReturnsExpand()
  {
    Assert.Equal(DecisionHint.Expand, DecisionTree.Suggest(Input(marketVacancies: 0, matured: 0, positive: 0)));
  }

  [Fact]
  public void Suggest_MarketBelowThresholdWithBadConversion_ReturnsExpand()
  {
    Assert.Equal(DecisionHint.Expand, DecisionTree.Suggest(Input(marketVacancies: 1, positive: 0, matured: 100)));
  }

  [Fact]
  public void Suggest_MarketExactlyAtThreshold_IsNotNarrow()
  {
    Assert.NotEqual(DecisionHint.Expand, DecisionTree.Suggest(Input(marketVacancies: 10, marketThreshold: 10)));
  }

  [Fact]
  public void Suggest_MaturedBelowMinSample_ReturnsNotEnoughData()
  {
    Assert.Equal(DecisionHint.NotEnoughData, DecisionTree.Suggest(Input(matured: 29, positive: 0, minSampleSize: 30)));
  }

  [Fact]
  public void Suggest_NoMaturedApplications_ReturnsNotEnoughData()
  {
    Assert.Equal(DecisionHint.NotEnoughData, DecisionTree.Suggest(Input(matured: 0, positive: 0)));
  }

  [Fact]
  public void Suggest_MaturedExactlyMinSample_HasEnoughData()
  {
    Assert.NotEqual(DecisionHint.NotEnoughData, DecisionTree.Suggest(Input(matured: 30, positive: 5, minSampleSize: 30)));
  }

  [Fact]
  public void Suggest_ConversionAboveThreshold_ReturnsContinue()
  {
    Assert.Equal(DecisionHint.Continue, DecisionTree.Suggest(Input(positive: 10, matured: 100, thresholdPermille: 30)));
  }

  [Fact]
  public void Suggest_ConversionExactlyAtThreshold_ReturnsContinue()
  {
    Assert.Equal(DecisionHint.Continue, DecisionTree.Suggest(Input(positive: 3, matured: 100, thresholdPermille: 30, minSampleSize: 30)));
  }

  [Fact]
  public void Suggest_ConversionJustBelowThreshold_ReturnsReviseCv()
  {
    Assert.Equal(DecisionHint.ReviseCv, DecisionTree.Suggest(Input(positive: 2, matured: 100, thresholdPermille: 30, minSampleSize: 30)));
  }

  [Fact]
  public void Suggest_NoPositiveWithEnoughData_ReturnsReviseCv()
  {
    Assert.Equal(DecisionHint.ReviseCv, DecisionTree.Suggest(Input(positive: 0, matured: 50)));
  }

  [Fact]
  public void Suggest_ZeroThresholdPermille_ReturnsContinueWhenEnoughData()
  {
    Assert.Equal(DecisionHint.Continue, DecisionTree.Suggest(Input(positive: 0, matured: 50, thresholdPermille: 0)));
  }
}
