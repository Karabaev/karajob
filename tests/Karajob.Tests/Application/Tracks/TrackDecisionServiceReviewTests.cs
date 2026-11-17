using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Tracks;

public class TrackDecisionServiceReviewTests : TrackDecisionServiceFixture
{
  [Fact]
  public async Task GetReviewAsync_UnknownTrack_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.GetReviewAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetReviewAsync_Active_MetricsEqualMetricsServiceForCurrentPeriod()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 3);
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(await Metrics.GetAsync(trackId), review.Metrics);
  }

  [Fact]
  public async Task GetReviewAsync_NoCheckIns_WellBeingIsEmpty()
  {
    var trackId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Null(review.WellBeing.LatestTenths);
    Assert.Null(review.WellBeing.TrendTenths);
    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_CheckIns_WellBeingHasLatestAndTrend()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 4, 3));

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(35, review.WellBeing.LatestTenths);
    Assert.Equal(15, review.WellBeing.TrendTenths);
    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_OtherTrackCheckIns_AreIgnored()
  {
    var trackId = await CreateActiveAsync("Mine");
    var other = await CreateActiveAsync("Other");
    await Decisions.SaveCheckInAsync(other, CheckIn(CurrentWeek, 1, 1));

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Null(review.WellBeing.LatestTenths);
  }

  [Fact]
  public async Task GetReviewAsync_DefaultSettings_EchoesDefaultWellBeingThreshold()
  {
    var trackId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(2, review.WellBeingThreshold);
  }

  [Fact]
  public async Task GetReviewAsync_CustomThreshold_IsEchoed()
  {
    await SetWellBeingThresholdAsync(4);
    var trackId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(4, review.WellBeingThreshold);
  }

  [Fact]
  public async Task GetReviewAsync_TwoLowConsecutiveCheckIns_IsLow()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 2, 1));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 1, 2));

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.True(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_LowCheckInsWithGapWeek_IsNotLow()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(TwoWeeksAgo, 1, 1));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 1, 1));

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.False(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_SameCheckInsWithHigherThreshold_BecomeLow()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 3, 4));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 4, 3));
    Assert.False((await Decisions.GetReviewAsync(trackId)).WellBeing.IsLow);

    await SetWellBeingThresholdAsync(4);

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.True(review.WellBeing.IsLow);
  }

  [Fact]
  public async Task GetReviewAsync_NarrowMarket_HintIsExpand()
  {
    var trackId = await CreateActiveAsync();

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(DecisionHint.Expand, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_TwoLowCheckIns_HintIsDisableEvenWithNarrowMarket()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 1, 1));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 2, 2));

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(DecisionHint.Disable, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_BroadMarketWithoutMaturedApplications_HintIsNotEnoughData()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 1);

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(DecisionHint.NotEnoughData, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_ConversionAtOrAboveThreshold_HintIsContinue()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4, conversionThresholdPermille: 30);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 1);
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(DecisionHint.Continue, review.Hint);
  }

  [Fact]
  public async Task GetReviewAsync_ConversionBelowThreshold_HintIsReviseCv()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4, conversionThresholdPermille: 30);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 1);
    await ApplyMaturedAsync(trackId, total: 4, positive: 0);

    var review = await Decisions.GetReviewAsync(trackId);

    Assert.Equal(DecisionHint.ReviseCv, review.Hint);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_ListsOnlyDraftsOrderedByName()
  {
    var trackId = await CreateActiveAsync("Active A");
    await CreateActiveAsync("Active B");
    await CreateTrackAsync("Zeta draft");
    await CreateTrackAsync("alpha draft");
    var paused = await CreateTrackAsync("Paused one");
    await SetStatusAsync(paused, TrackStatus.Paused);
    var disabled = await CreateTrackAsync("Disabled one");
    await SetStatusAsync(disabled, TrackStatus.Disabled);

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Equal(["alpha draft", "Zeta draft"], options.Drafts.Select(d => d.Name));
  }

  [Fact]
  public async Task GetDisableOptionsAsync_DraftOption_CarriesTrackId()
  {
    var trackId = await CreateActiveAsync();
    var draftId = await CreateTrackAsync("Draft one");

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Equal(draftId, Assert.Single(options.Drafts).Id);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_AnotherProfilesDrafts_AreExcluded()
  {
    var trackId = await CreateActiveAsync();
    var other = await CreateProfileAsync();
    await CreateTrackAsync("Foreign draft", other);

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Empty(options.Drafts);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_NoDrafts_ReturnsEmptyList()
  {
    var trackId = await CreateActiveAsync();

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Empty(options.Drafts);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_ActiveAndExpanded_CountAfterExcludesThisOne()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C", status: TrackStatus.Expanded);

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Equal(2, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_PausedDraftAndDisabled_AreNotCounted()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("Paused", status: TrackStatus.Paused);
    await CreateActiveAsync("Disabled", status: TrackStatus.Disabled);
    await CreateTrackAsync("Draft");

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Equal(0, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_AnotherProfilesActive_AreNotCounted()
  {
    var trackId = await CreateActiveAsync("A");
    var other = await CreateProfileAsync();
    await CreateActiveAsync("Foreign", other);

    var options = await Decisions.GetDisableOptionsAsync(trackId);

    Assert.Equal(0, options.ActiveCountAfter);
  }

  [Fact]
  public async Task GetDisableOptionsAsync_UnknownTrack_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.GetDisableOptionsAsync(Guid.NewGuid()));
  }
}
