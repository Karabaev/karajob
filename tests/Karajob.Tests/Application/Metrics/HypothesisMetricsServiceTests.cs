using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Goals;
using Karajob.Domain.Metrics;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Application.Metrics;

public class HypothesisMetricsServiceTests : HypothesisMetricsServiceFixture
{
  private static readonly DateTime MaturedAt = Now.AddDays(-20);

  [Fact]
  public async Task GetAsync_EmptyHypothesis_ReturnsZeroCountsAndNoRate()
  {
    var hypothesisId = await CreateHypothesisAsync();

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(hypothesisId, metrics.HypothesisId);
    Assert.Equal(0, metrics.Market.VacanciesThisWeek);
    Assert.Equal(0, metrics.WeeklyApplications);
    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(0, metrics.Conversion.Matured);
    Assert.Null(metrics.Conversion.Rate);
    Assert.Null(metrics.Conversion.Interval);
    Assert.True(metrics.Conversion.IsInsufficientData);
  }

  [Fact]
  public async Task GetAsync_DefaultGoal_EchoesDefaultThresholds()
  {
    var hypothesisId = await CreateHypothesisAsync();

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(SearchGoalDefaults.MarketThresholdPerWeek, metrics.Market.ThresholdPerWeek);
    Assert.Equal(SearchGoalDefaults.ConversionThresholdPermille, metrics.Conversion.ThresholdPermille);
    Assert.Equal(SearchGoalDefaults.MinSampleSize, metrics.Conversion.MinSampleSize);
    Assert.Equal(SearchGoalDefaults.MaturationDays, metrics.Conversion.MaturationDays);
  }

  [Fact]
  public async Task GetAsync_CustomGoal_EchoesCustomThresholds()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await UpdateGoalAsync(marketThresholdPerWeek: 3, conversionThresholdPermille: 50, minSampleSize: 7, maturationDays: 10);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(3, metrics.Market.ThresholdPerWeek);
    Assert.Equal(50, metrics.Conversion.ThresholdPermille);
    Assert.Equal(7, metrics.Conversion.MinSampleSize);
    Assert.Equal(10, metrics.Conversion.MaturationDays);
  }

  [Fact]
  public async Task GetAsync_NoExplicitAsOf_UsesClockAndItsWeek()
  {
    var hypothesisId = await CreateHypothesisAsync();

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(Now, metrics.AsOf);
    Assert.Equal(CalendarWeek.Containing(Now, TimeZoneInfo.Utc), metrics.Week);
    Assert.Equal(WeekStart, metrics.Week.Start);
  }

  [Fact]
  public async Task GetAsync_ApplicationExactlyMaturationDaysOld_IsMatured()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-SearchGoalDefaults.MaturationDays));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ApplicationOneSecondYoungerThanMaturation_IsNotMatured()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-SearchGoalDefaults.MaturationDays).AddSeconds(1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_CustomMaturationDays_UsesThemForMaturity()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await UpdateGoalAsync(maturationDays: 5);
    await ApplyAsync(hypothesisId, Now.AddDays(-5));
    await ApplyAsync(hypothesisId, Now.AddDays(-4));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredSundayBeforeWeek_IsNotCountedInMarket()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateVacancyAsync(hypothesisId, WeekStart.AddMinutes(-1));

    var metrics = await Metrics.GetAsync(hypothesisId, WeekStart.AddDays(-30));

    Assert.Equal(0, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredMondayMidnight_IsCountedInMarket()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateVacancyAsync(hypothesisId, WeekStart);

    var metrics = await Metrics.GetAsync(hypothesisId, WeekStart.AddDays(-30));

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacanciesOnSeveralDaysOfWeek_AreAllCounted()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await CreateVacancyAsync(hypothesisId, Utc(11, 9, 8));
    await CreateVacancyAsync(hypothesisId, Utc(11, 10, 12));
    await CreateVacancyAsync(hypothesisId, Utc(11, 12, 9));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(3, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyDismissedFromInbox_IsStillCountedInMarket()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await CreateVacancyAsync(hypothesisId, Utc(11, 10, 12));
    await Service.DismissAsync(vacancyId, new DismissInput(DismissReason.WrongStack, null));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyMatchedToAnotherHypothesis_IsNotCounted()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var otherId = await CreateStartedHypothesisAsync(name: "Other");
    await CreateVacancyAsync(otherId, Utc(11, 10, 12));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyMatchedToTwoHypotheses_IsCountedForEach()
  {
    var firstId = await CreateStartedHypothesisAsync();
    var secondId = await CreateStartedHypothesisAsync(name: "Other");
    var vacancyId = await CreateVacancyAsync(firstId, Utc(11, 10, 12));
    await MutateAsync(vacancyId, v => v.SetMatch(secondId, null, null, MatchAuthor.Human));

    var first = await Metrics.GetAsync(firstId);
    var second = await Metrics.GetAsync(secondId);

    Assert.Equal(1, first.Market.VacanciesThisWeek);
    Assert.Equal(1, second.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_MatchesWithAndWithoutScore_AreBothCounted()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var scored = await CreateVacancyAsync(hypothesisId, Utc(11, 10, 12));
    await CreateVacancyAsync(hypothesisId, Utc(11, 11, 12));
    await MutateAsync(scored, v => v.SetMatch(hypothesisId, 80, "fits", MatchAuthor.Ai));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(2, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_ApplicationBeforeWeek_IsNotCountedInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, WeekStart.AddMinutes(-1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_ApplicationOnMondayMidnight_IsCountedInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, WeekStart);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_ApplicationsOfAnotherHypothesis_AreNotCountedInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var otherId = await CreateStartedHypothesisAsync(name: "Other");
    await ApplyAsync(otherId, Utc(11, 10, 12));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  public async Task GetAsync_PositiveEventOnLaterDay_CountsApplicationAsPositive(ApplicationEventType type)
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, type, MaturedAt.AddDays(2));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventTheDayBeforeApplying_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddSeconds(-1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventAtMidnightOfApplyDay_IsPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.TechInterview, MaturedAt.Date);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_OnlyNoteEvent_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Note, MaturedAt.AddDays(1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferOutcomeAndNoEvents_IsPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, MaturedAt, afterApply: v => v.Close(ApplicationOutcome.Offer, MaturedAt.AddDays(3)));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_ClosedWithRejectedOutcomeAndNoEvents_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, MaturedAt, afterApply: v => v.Close(ApplicationOutcome.Rejected, MaturedAt.AddDays(3)));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventAfterAsOf_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Now.AddDays(-5);
    var vacancyId = await ApplyAsync(hypothesisId, asOf.AddDays(-20));
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, asOf.AddDays(1));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferAfterAsOf_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Now.AddDays(-5);
    await ApplyAsync(hypothesisId, asOf.AddDays(-20), afterApply: v => v.Close(ApplicationOutcome.Offer, asOf.AddDays(1)));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferBeforeAsOf_IsPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Now.AddDays(-5);
    await ApplyAsync(hypothesisId, asOf.AddDays(-20), afterApply: v => v.Close(ApplicationOutcome.Offer, asOf.AddDays(-1)));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_OfferReopenedToApplied_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, MaturedAt, afterApply: v =>
    {
      v.Close(ApplicationOutcome.Offer, MaturedAt.AddDays(3));
      v.MoveTo(VacancyStage.Applied, MaturedAt.AddDays(4));
    });

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_MovedToInterviewingWithoutEvent_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, MaturedAt, afterApply: v => v.MoveTo(VacancyStage.Interviewing, MaturedAt.AddDays(1)));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_PositiveButNotMaturedApplication_IsExcludedFromPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var young = Now.AddDays(-3);
    var vacancyId = await ApplyAsync(hypothesisId, young);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, young.AddDays(1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(0, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveAndNegativeApplications_ComputesRateAndWilsonInterval()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var positive = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(positive, ApplicationEventType.Screening, MaturedAt.AddDays(1));
    await ApplyAsync(hypothesisId, MaturedAt.AddDays(-1));
    await ApplyAsync(hypothesisId, MaturedAt.AddDays(-2));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Positive);
    Assert.Equal(3, metrics.Conversion.Matured);
    Assert.Equal(1 / 3.0, metrics.Conversion.Rate!.Value, 10);
    Assert.Equal(WilsonInterval.Compute(1, 3), metrics.Conversion.Interval);
  }

  [Fact]
  public async Task GetAsync_MaturedBelowMinSampleSize_IsInsufficientData()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await UpdateGoalAsync(minSampleSize: 2);
    await ApplyAsync(hypothesisId, MaturedAt);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.True(metrics.Conversion.IsInsufficientData);
  }

  [Fact]
  public async Task GetAsync_MaturedEqualsMinSampleSize_IsSufficientData()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await UpdateGoalAsync(minSampleSize: 2);
    await ApplyAsync(hypothesisId, MaturedAt);
    await ApplyAsync(hypothesisId, MaturedAt.AddDays(-1));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.False(metrics.Conversion.IsInsufficientData);
  }

  [Fact]
  public async Task GetAsync_NoPeriodStart_DefaultsToStartedAt()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var startedAt = Now.AddDays(-30);
    await MutateHypothesisAsync(hypothesisId, h => h.StartedAt = startedAt);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(startedAt, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_ApplicationBeforeStartedAt_IsExcludedFromConversion()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await MutateHypothesisAsync(hypothesisId, h => h.StartedAt = Now.AddDays(-30));
    await ApplyAsync(hypothesisId, Now.AddDays(-40));
    await ApplyAsync(hypothesisId, Now.AddDays(-25));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ApplicationExactlyAtPeriodStart_IsIncluded()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var startedAt = Now.AddDays(-30);
    await MutateHypothesisAsync(hypothesisId, h => h.StartedAt = startedAt);
    await ApplyAsync(hypothesisId, startedAt);

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ExplicitPeriodStart_NarrowsConversionBeyondStartedAt()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var periodStart = Now.AddDays(-25);
    await ApplyAsync(hypothesisId, Now.AddDays(-30));
    await ApplyAsync(hypothesisId, Now.AddDays(-20));

    var metrics = await Metrics.GetAsync(hypothesisId, periodStart);

    Assert.Equal(periodStart, metrics.PeriodStart);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PeriodStartInsideWeek_NarrowsWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 9, 12));
    await ApplyAsync(hypothesisId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(hypothesisId, Utc(11, 10));

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_PeriodStartInsideWeek_NarrowsMarket()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await CreateVacancyAsync(hypothesisId, Utc(11, 9, 12));
    await CreateVacancyAsync(hypothesisId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(hypothesisId, Utc(11, 10));

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_StartedAtWithinWeek_NarrowsWeeklyApplicationsByDefault()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await MutateHypothesisAsync(hypothesisId, h => h.StartedAt = Utc(11, 11));
    await ApplyAsync(hypothesisId, Utc(11, 10, 12));
    await ApplyAsync(hypothesisId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_HypothesisWithoutStartedAt_FallsBackToCreatedAt()
  {
    var createdAt = Now.AddDays(-30);
    var hypothesisId = await CreateHypothesisCreatedAtAsync(createdAt);
    await ApplyAsync(hypothesisId, Now.AddDays(-40));
    await ApplyAsync(hypothesisId, Now.AddDays(-25));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(createdAt, metrics.PeriodStart);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public async Task GetAsync_NonUtcPeriodStart_Throws(DateTimeKind kind)
  {
    var hypothesisId = await CreateHypothesisAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(
      () => Metrics.GetAsync(hypothesisId, DateTime.SpecifyKind(Now.AddDays(-5), kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public async Task GetAsync_NonUtcAsOf_Throws(DateTimeKind kind)
  {
    var hypothesisId = await CreateHypothesisAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(
      () => Metrics.GetAsync(hypothesisId, null, DateTime.SpecifyKind(Now, kind)));
  }

  [Fact]
  public async Task GetAsync_PeriodStartAfterAsOf_Throws()
  {
    var hypothesisId = await CreateHypothesisAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(() => Metrics.GetAsync(hypothesisId, Now.AddSeconds(1), Now));
  }

  [Fact]
  public async Task GetAsync_PeriodStartEqualToAsOf_DoesNotThrow()
  {
    var hypothesisId = await CreateHypothesisAsync();

    var metrics = await Metrics.GetAsync(hypothesisId, Now, Now);

    Assert.Equal(Now, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_AsOfInThePast_UsesThatWeek()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Now.AddDays(-7);

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(asOf, metrics.AsOf);
    Assert.Equal(Utc(11, 2), metrics.Week.Start);
    Assert.Equal(Utc(11, 9), metrics.Week.End);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredAfterAsOf_IsInvisibleInMarket()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Utc(11, 5, 10);
    await CreateVacancyAsync(hypothesisId, Utc(11, 4, 12));
    await CreateVacancyAsync(hypothesisId, asOf);
    await CreateVacancyAsync(hypothesisId, asOf.AddSeconds(1));
    await CreateVacancyAsync(hypothesisId, Utc(11, 6, 12));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(2, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_ApplicationAfterAsOf_IsInvisibleInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Utc(11, 5, 10);
    await ApplyAsync(hypothesisId, Utc(11, 3, 12));
    await ApplyAsync(hypothesisId, asOf.AddSeconds(1));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_AsOfInThePast_MaturityIsRelativeToAsOf()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var asOf = Utc(11, 5, 10);
    await ApplyAsync(hypothesisId, asOf.AddDays(-21));
    await ApplyAsync(hypothesisId, asOf.AddDays(-11));
    await ApplyAsync(hypothesisId, asOf.AddDays(2));

    var metrics = await Metrics.GetAsync(hypothesisId, null, asOf);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_UnknownHypothesis_Throws()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Metrics.GetAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetAsync_HypothesisOfAnotherUser_Throws()
  {
    var otherUserId = await CreateProfileAsync();
    var foreignHypothesisId = await CreateHypothesisAsync(userId: otherUserId);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Metrics.GetAsync(foreignHypothesisId));
  }

  [Fact]
  public async Task GetAsync_OtherUsersData_DoesNotLeakIntoOwnMetrics()
  {
    var otherUserId = await CreateProfileAsync();
    var ownId = await CreateStartedHypothesisAsync();
    var foreignId = await CreateStartedHypothesisAsync(otherUserId);
    await CreateVacancyAsync(foreignId, Utc(11, 10, 12), otherUserId);
    await ApplyAsync(foreignId, MaturedAt, otherUserId);
    await ApplyAsync(foreignId, Utc(11, 10, 12), otherUserId);

    var own = await Metrics.GetAsync(ownId);
    var foreign = await MetricsFor(otherUserId).GetAsync(foreignId);

    Assert.Equal(0, own.Market.VacanciesThisWeek);
    Assert.Equal(0, own.Conversion.Matured);
    Assert.Equal(0, own.WeeklyApplications);
    Assert.Equal(1, foreign.Conversion.Matured);
    Assert.Equal(1, foreign.WeeklyApplications);
  }

  [Fact]
  public async Task GetActivityAsync_NoApplications_ReturnsZeroWithDefaultTarget()
  {
    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(0, activity.Applications);
    Assert.Equal(SearchGoalDefaults.WeeklyApplicationTarget, activity.WeeklyTarget);
    Assert.Equal(CalendarWeek.Containing(Now, TimeZoneInfo.Utc), activity.Week);
  }

  [Fact]
  public async Task GetActivityAsync_ApplicationsOfAllHypotheses_AreSummedForTheWeek()
  {
    var firstId = await CreateStartedHypothesisAsync();
    var secondId = await CreateStartedHypothesisAsync(name: "Other");
    await ApplyAsync(firstId, Utc(11, 9, 9));
    await ApplyAsync(firstId, Utc(11, 10, 9));
    await ApplyAsync(secondId, Utc(11, 11, 9));

    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(3, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_ApplicationsOutsideWeekOrAfterAsOf_AreNotCounted()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, WeekStart.AddMinutes(-1));
    await ApplyAsync(hypothesisId, Now.AddSeconds(1));
    await ApplyAsync(hypothesisId, Now);

    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(1, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_ExplicitAsOf_UsesThatWeek()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 3, 12));
    await ApplyAsync(hypothesisId, Utc(11, 10, 12));

    var activity = await Metrics.GetActivityAsync(Utc(11, 5, 10));

    Assert.Equal(Utc(11, 2), activity.Week.Start);
    Assert.Equal(1, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_CustomTarget_IsEchoed()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 7);

    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(7, activity.WeeklyTarget);
  }

  [Fact]
  public async Task GetActivityAsync_NonUtcAsOf_Throws()
  {
    await Assert.ThrowsAnyAsync<ArgumentException>(
      () => Metrics.GetActivityAsync(DateTime.SpecifyKind(Now, DateTimeKind.Unspecified)));
  }

  [Fact]
  public async Task GetActivityAsync_OtherUsersApplications_AreNotCounted()
  {
    var otherUserId = await CreateProfileAsync();
    var ownId = await CreateStartedHypothesisAsync();
    var foreignId = await CreateStartedHypothesisAsync(otherUserId);
    await ApplyAsync(ownId, Utc(11, 10, 9));
    await ApplyAsync(foreignId, Utc(11, 10, 9), otherUserId);
    await ApplyAsync(foreignId, Utc(11, 11, 9), otherUserId);

    var own = await Metrics.GetActivityAsync();
    var foreign = await MetricsFor(otherUserId).GetActivityAsync();

    Assert.Equal(1, own.Applications);
    Assert.Equal(2, foreign.Applications);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_WeekStartsOnLocalMonday()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(hypothesisId, asOf: Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 16), metrics.Week.FirstDay);
    Assert.Equal(Utc(11, 15, 21), metrics.Week.Start);
    Assert.Equal(Utc(11, 22, 21), metrics.Week.End);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_ApplicationAfterLocalMondayMidnight_IsCountedInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 15, 21, 30));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(hypothesisId, asOf: Utc(11, 15, 22));

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_ApplicationOnLocalSunday_IsNotCountedInWeeklyApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 15, 20));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(hypothesisId, asOf: Utc(11, 15, 22));

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_UtcZoneSameInstants_CountsApplicationOnUtcSunday()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 15, 20));

    var metrics = await MetricsInZone("UTC").GetAsync(hypothesisId, asOf: Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 9), metrics.Week.FirstDay);
    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetActivityAsync_MoscowZone_CountsOnlyApplicationsSinceLocalMonday()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 15, 21, 30));
    await ApplyAsync(hypothesisId, Utc(11, 15, 20));

    var activity = await MetricsInZone("Europe/Moscow").GetActivityAsync(Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 16), activity.Week.FirstDay);
    Assert.Equal(1, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_UtcZoneSameInstants_CountsBothApplications()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyAsync(hypothesisId, Utc(11, 15, 21, 30));
    await ApplyAsync(hypothesisId, Utc(11, 15, 20));

    var activity = await MetricsInZone("UTC").GetActivityAsync(Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 9), activity.Week.FirstDay);
    Assert.Equal(2, activity.Applications);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_PositiveEventAtLocalMidnightOfApplyDay_IsPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddHours(-3));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_PositiveEventBeforeLocalApplyDay_IsNotPositive()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();
    var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddHours(-3).AddSeconds(-1));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(hypothesisId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }
}
