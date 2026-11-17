using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class WellBeingTests
{
  private static readonly DateOnly CurrentWeek = new(2026, 11, 9);
  private static readonly DateOnly PreviousWeek = CurrentWeek.AddDays(-7);
  private static readonly DateOnly TwoWeeksAgo = CurrentWeek.AddDays(-14);
  private static readonly DateOnly ThreeWeeksAgo = CurrentWeek.AddDays(-21);

  private static WellBeingCheckIn CheckIn(DateOnly week, int motivation, int confidence)
  {
    return new WellBeingCheckIn(week, motivation, confidence);
  }

  [Theory]
  [InlineData(2, 3, 25)]
  [InlineData(1, 1, 10)]
  [InlineData(5, 5, 50)]
  [InlineData(4, 5, 45)]
  public void AverageTenths_Scores_ReturnsAverageTimesTen(int motivation, int confidence, int expected)
  {
    Assert.Equal(expected, WellBeing.AverageTenths(motivation, confidence));
  }

  [Fact]
  public void Summarize_NoCheckIns_ReturnsEmptySummary()
  {
    var summary = WellBeing.Summarize([], CurrentWeek, 2);

    Assert.Null(summary.LatestTenths);
    Assert.Null(summary.TrendTenths);
    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_SingleCheckIn_HasLatestButNoTrend()
  {
    var summary = WellBeing.Summarize([CheckIn(CurrentWeek, 2, 3)], CurrentWeek, 2);

    Assert.Equal(25, summary.LatestTenths);
    Assert.Null(summary.TrendTenths);
  }

  [Fact]
  public void Summarize_SingleLowCheckIn_IsNotLow()
  {
    var summary = WellBeing.Summarize([CheckIn(CurrentWeek, 1, 1)], CurrentWeek, 2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_TwoCheckIns_TrendIsLatestMinusPrevious()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 2, 2), CheckIn(CurrentWeek, 4, 3)], CurrentWeek, 2);

    Assert.Equal(35, summary.LatestTenths);
    Assert.Equal(15, summary.TrendTenths);
  }

  [Fact]
  public void Summarize_FallingScores_TrendIsNegative()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 4, 4), CheckIn(CurrentWeek, 2, 3)], CurrentWeek, 2);

    Assert.Equal(-15, summary.TrendTenths);
  }

  [Fact]
  public void Summarize_UnorderedInput_SortsByWeek()
  {
    var summary = WellBeing.Summarize(
      [CheckIn(CurrentWeek, 5, 5), CheckIn(ThreeWeeksAgo, 1, 1), CheckIn(PreviousWeek, 3, 3)],
      CurrentWeek,
      2);

    Assert.Equal(50, summary.LatestTenths);
    Assert.Equal(20, summary.TrendTenths);
  }

  [Fact]
  public void Summarize_PreviousCheckInAfterGap_StillUsedForTrend()
  {
    var summary = WellBeing.Summarize([CheckIn(TwoWeeksAgo, 2, 2), CheckIn(CurrentWeek, 4, 4)], CurrentWeek, 2);

    Assert.Equal(40, summary.LatestTenths);
    Assert.Equal(20, summary.TrendTenths);
  }

  [Fact]
  public void Summarize_TwoLowConsecutiveWeeksIncludingCurrent_IsLow()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 2, 1), CheckIn(CurrentWeek, 1, 2)], CurrentWeek, 2);

    Assert.True(summary.IsLow);
  }

  [Fact]
  public void Summarize_LatestIsPreviousWeek_StillCountsAsCurrent()
  {
    var summary = WellBeing.Summarize([CheckIn(TwoWeeksAgo, 1, 1), CheckIn(PreviousWeek, 2, 2)], CurrentWeek, 2);

    Assert.True(summary.IsLow);
  }

  [Fact]
  public void Summarize_AveragesExactlyAtThreshold_IsLow()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 2, 2), CheckIn(CurrentWeek, 2, 2)], CurrentWeek, 2);

    Assert.True(summary.IsLow);
  }

  [Fact]
  public void Summarize_LatestAverageJustAboveThreshold_IsNotLow()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 2, 2), CheckIn(CurrentWeek, 2, 3)], CurrentWeek, 2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_PreviousAverageJustAboveThreshold_IsNotLow()
  {
    var summary = WellBeing.Summarize([CheckIn(PreviousWeek, 3, 2), CheckIn(CurrentWeek, 1, 1)], CurrentWeek, 2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_GapWeekBetweenCheckIns_IsNotLow()
  {
    var summary = WellBeing.Summarize([CheckIn(TwoWeeksAgo, 1, 1), CheckIn(CurrentWeek, 1, 1)], CurrentWeek, 2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_LatestCheckInIsStale_IsNotLow()
  {
    var summary = WellBeing.Summarize([CheckIn(ThreeWeeksAgo, 1, 1), CheckIn(TwoWeeksAgo, 1, 1)], CurrentWeek, 2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_StaleLatest_StillReportsLatestAndTrend()
  {
    var summary = WellBeing.Summarize([CheckIn(ThreeWeeksAgo, 1, 1), CheckIn(TwoWeeksAgo, 2, 2)], CurrentWeek, 2);

    Assert.Equal(20, summary.LatestTenths);
    Assert.Equal(10, summary.TrendTenths);
  }

  [Fact]
  public void Summarize_OlderLowCheckInsDoNotMatterWhenLatestRecovered_IsNotLow()
  {
    var summary = WellBeing.Summarize(
      [CheckIn(TwoWeeksAgo, 1, 1), CheckIn(PreviousWeek, 1, 1), CheckIn(CurrentWeek, 5, 5)],
      CurrentWeek,
      2);

    Assert.False(summary.IsLow);
  }

  [Fact]
  public void Summarize_HigherThreshold_TurnsLowSignalOn()
  {
    WellBeingCheckIn[] checkIns = [CheckIn(PreviousWeek, 3, 4), CheckIn(CurrentWeek, 4, 3)];

    Assert.False(WellBeing.Summarize(checkIns, CurrentWeek, 2).IsLow);
    Assert.True(WellBeing.Summarize(checkIns, CurrentWeek, 4).IsLow);
  }
}
