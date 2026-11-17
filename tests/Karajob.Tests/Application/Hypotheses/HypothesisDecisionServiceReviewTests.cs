using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisDecisionServiceReviewTests : HypothesisDecisionServiceFixture
{
  [Fact]
  public async Task GetReviewAsync_UnknownHypothesis_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.GetReviewAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetReviewAsync_Active_MetricsEqualMetricsServiceForCurrentPeriod()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 3);
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(await Metrics.GetAsync(hypothesisId), review.Metrics);
  }

  [Fact]
  public async Task GetReviewAsync_NoCheckIns_WellBeingIsEmpty()
  {
    var hypothesisId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Null(review.WellBeing.LatestTenths);
    Assert.Null(review.WellBeing.TrendTenths);
    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_CheckIns_WellBeingHasLatestAndTrend()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 4, 3));

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(35, review.WellBeing.LatestTenths);
    Assert.Equal(15, review.WellBeing.TrendTenths);
    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_OtherHypothesisCheckIns_AreIgnored()
  {
    var hypothesisId = await CreateActiveAsync("Mine");
    var other = await CreateActiveAsync("Other");
    await Decisions.SaveCheckInAsync(other, CheckIn(CurrentWeek, 1, 1));

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Null(review.WellBeing.LatestTenths);
  }

  [Fact]
  public async Task GetReviewAsync_DefaultSettings_EchoesDefaultWellBeingThreshold()
  {
    var hypothesisId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(2, review.WellBeingThreshold);
  }

  [Fact]
  public async Task GetReviewAsync_CustomThreshold_IsEchoed()
  {
    await SetWellBeingThresholdAsync(4);
    var hypothesisId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(4, review.WellBeingThreshold);
  }

  [Fact]
  public async Task GetReviewAsync_TwoLowConsecutiveCheckIns_IsLow()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 2, 1));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 1, 2));

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.True(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_LowCheckInsWithGapWeek_IsNotLow()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(TwoWeeksAgo, 1, 1));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 1, 1));

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_SameCheckInsWithHigherThreshold_BecomeLow()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 3, 4));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 4, 3));
    Assert.False((await Decisions.GetReviewAsync(hypothesisId)).WellBeing.IsLow);

    await SetWellBeingThresholdAsync(4);

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.True(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_NarrowMarket_HintIsExpand()
  {
    var hypothesisId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(DecisionHint.Expand, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_TwoLowCheckIns_HintIsDisableEvenWithNarrowMarket()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 1, 1));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 2, 2));

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(DecisionHint.Disable, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_BroadMarketWithoutMaturedApplications_HintIsNotEnoughData()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 1);

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(DecisionHint.NotEnoughData, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_ConversionAtOrAboveThreshold_HintIsContinue()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4, conversionThresholdPermille: 30);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 1);
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(DecisionHint.Continue, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_ConversionBelowThreshold_HintIsReviseCv()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4, conversionThresholdPermille: 30);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 1);
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 0);

    var review = await Decisions.GetReviewAsync(hypothesisId);

    Assert.Equal(DecisionHint.ReviseCv, review.Hint);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_ListsOnlyDraftsOrderedByName()
  {
    var hypothesisId = await CreateActiveAsync("Active A");
    await CreateActiveAsync("Active B");
    await CreateHypothesisAsync("Zeta draft");
    await CreateHypothesisAsync("alpha draft");
    var paused = await CreateHypothesisAsync("Paused one");
    await SetStatusAsync(paused, HypothesisStatus.Paused);
    var disabled = await CreateHypothesisAsync("Disabled one");
    await SetStatusAsync(disabled, HypothesisStatus.Disabled);

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Equal(["alpha draft", "Zeta draft"], options.Drafts.Select(d => d.Name));
  }

  [Fact]
  public async Task GetDisableOptionsAsync_DraftOption_CarriesHypothesisId()
  {
    var hypothesisId = await CreateActiveAsync();
    var draftId = await CreateHypothesisAsync("Draft one");

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Equal(draftId, Assert.Single(options.Drafts).Id);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_AnotherProfilesDrafts_AreExcluded()
  {
    var hypothesisId = await CreateActiveAsync();
    var other = await CreateProfileAsync();
    await CreateHypothesisAsync("Foreign draft", other);

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Empty(options.Drafts);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_NoDrafts_ReturnsEmptyList()
  {
    var hypothesisId = await CreateActiveAsync();

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Empty(options.Drafts);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_ActiveAndExpanded_CountAfterExcludesThisOne()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C", status: HypothesisStatus.Expanded);

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Equal(2, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_PausedDraftAndDisabled_AreNotCounted()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("Paused", status: HypothesisStatus.Paused);
    await CreateActiveAsync("Disabled", status: HypothesisStatus.Disabled);
    await CreateHypothesisAsync("Draft");

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Equal(0, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_AnotherProfilesActive_AreNotCounted()
  {
    var hypothesisId = await CreateActiveAsync("A");
    var other = await CreateProfileAsync();
    await CreateActiveAsync("Foreign", other);

    var options = await Decisions.GetDisableOptionsAsync(hypothesisId);

    Assert.Equal(0, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_UnknownHypothesis_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.GetDisableOptionsAsync(Guid.NewGuid()));
  }
}
