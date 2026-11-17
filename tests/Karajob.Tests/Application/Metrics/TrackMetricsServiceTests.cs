using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Goals;
using Karajob.Domain.Metrics;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Application.Metrics;

public class TrackMetricsServiceTests : TrackMetricsServiceFixture
{
  private static readonly DateTime MaturedAt = Now.AddDays(-20);

  [Fact]
  public async Task GetAsync_EmptyTrack_ReturnsZeroCountsAndNoRate()
  {
    var trackId = await CreateTrackAsync();

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(trackId, metrics.TrackId);
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
    var trackId = await CreateTrackAsync();

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(SearchGoalDefaults.MarketThresholdPerWeek, metrics.Market.ThresholdPerWeek);
    Assert.Equal(SearchGoalDefaults.ConversionThresholdPermille, metrics.Conversion.ThresholdPermille);
    Assert.Equal(SearchGoalDefaults.MinSampleSize, metrics.Conversion.MinSampleSize);
    Assert.Equal(SearchGoalDefaults.MaturationDays, metrics.Conversion.MaturationDays);
  }

  [Fact]
  public async Task GetAsync_CustomGoal_EchoesCustomThresholds()
  {
    var trackId = await CreateTrackAsync();
    await UpdateGoalAsync(marketThresholdPerWeek: 3, conversionThresholdPermille: 50, minSampleSize: 7, maturationDays: 10);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(3, metrics.Market.ThresholdPerWeek);
    Assert.Equal(50, metrics.Conversion.ThresholdPermille);
    Assert.Equal(7, metrics.Conversion.MinSampleSize);
    Assert.Equal(10, metrics.Conversion.MaturationDays);
  }

  [Fact]
  public async Task GetAsync_NoExplicitAsOf_UsesClockAndItsWeek()
  {
    var trackId = await CreateTrackAsync();

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(Now, metrics.AsOf);
    Assert.Equal(CalendarWeek.Containing(Now, TimeZoneInfo.Utc), metrics.Week);
    Assert.Equal(WeekStart, metrics.Week.Start);
  }

  [Fact]
  public async Task GetAsync_ApplicationExactlyMaturationDaysOld_IsMatured()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Now.AddDays(-SearchGoalDefaults.MaturationDays));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ApplicationOneSecondYoungerThanMaturation_IsNotMatured()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Now.AddDays(-SearchGoalDefaults.MaturationDays).AddSeconds(1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_CustomMaturationDays_UsesThemForMaturity()
  {
    var trackId = await CreateStartedTrackAsync();
    await UpdateGoalAsync(maturationDays: 5);
    await ApplyAsync(trackId, Now.AddDays(-5));
    await ApplyAsync(trackId, Now.AddDays(-4));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredSundayBeforeWeek_IsNotCountedInMarket()
  {
    var trackId = await CreateTrackAsync();
    await CreateVacancyAsync(trackId, WeekStart.AddMinutes(-1));

    var metrics = await Metrics.GetAsync(trackId, WeekStart.AddDays(-30));

    Assert.Equal(0, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredMondayMidnight_IsCountedInMarket()
  {
    var trackId = await CreateTrackAsync();
    await CreateVacancyAsync(trackId, WeekStart);

    var metrics = await Metrics.GetAsync(trackId, WeekStart.AddDays(-30));

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacanciesOnSeveralDaysOfWeek_AreAllCounted()
  {
    var trackId = await CreateStartedTrackAsync();
    await CreateVacancyAsync(trackId, Utc(11, 9, 8));
    await CreateVacancyAsync(trackId, Utc(11, 10, 12));
    await CreateVacancyAsync(trackId, Utc(11, 12, 9));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(3, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyDismissedFromInbox_IsStillCountedInMarket()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await CreateVacancyAsync(trackId, Utc(11, 10, 12));
    await Service.DismissAsync(vacancyId, new DismissInput(DismissReason.WrongStack, null));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyMatchedToAnotherTrack_IsNotCounted()
  {
    var trackId = await CreateStartedTrackAsync();
    var otherId = await CreateStartedTrackAsync(name: "Other");
    await CreateVacancyAsync(otherId, Utc(11, 10, 12));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_VacancyMatchedToTwoTracks_IsCountedForEach()
  {
    var firstId = await CreateStartedTrackAsync();
    var secondId = await CreateStartedTrackAsync(name: "Other");
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
    var trackId = await CreateStartedTrackAsync();
    var scored = await CreateVacancyAsync(trackId, Utc(11, 10, 12));
    await CreateVacancyAsync(trackId, Utc(11, 11, 12));
    await MutateAsync(scored, v => v.SetMatch(trackId, 80, "fits", MatchAuthor.Ai));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(2, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_ApplicationBeforeWeek_IsNotCountedInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, WeekStart.AddMinutes(-1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_ApplicationOnMondayMidnight_IsCountedInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, WeekStart);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_ApplicationsOfAnotherTrack_AreNotCountedInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    var otherId = await CreateStartedTrackAsync(name: "Other");
    await ApplyAsync(otherId, Utc(11, 10, 12));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  public async Task GetAsync_PositiveEventOnLaterDay_CountsApplicationAsPositive(ApplicationEventType type)
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, type, MaturedAt.AddDays(2));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventTheDayBeforeApplying_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddSeconds(-1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventAtMidnightOfApplyDay_IsPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.TechInterview, MaturedAt.Date);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_OnlyNoteEvent_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Note, MaturedAt.AddDays(1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferOutcomeAndNoEvents_IsPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, MaturedAt, afterApply: v => v.Close(ApplicationOutcome.Offer, MaturedAt.AddDays(3)));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_ClosedWithRejectedOutcomeAndNoEvents_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, MaturedAt, afterApply: v => v.Close(ApplicationOutcome.Rejected, MaturedAt.AddDays(3)));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveEventAfterAsOf_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Now.AddDays(-5);
    var vacancyId = await ApplyAsync(trackId, asOf.AddDays(-20));
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, asOf.AddDays(1));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferAfterAsOf_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Now.AddDays(-5);
    await ApplyAsync(trackId, asOf.AddDays(-20), afterApply: v => v.Close(ApplicationOutcome.Offer, asOf.AddDays(1)));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ClosedWithOfferBeforeAsOf_IsPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Now.AddDays(-5);
    await ApplyAsync(trackId, asOf.AddDays(-20), afterApply: v => v.Close(ApplicationOutcome.Offer, asOf.AddDays(-1)));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_OfferReopenedToApplied_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, MaturedAt, afterApply: v =>
    {
      v.Close(ApplicationOutcome.Offer, MaturedAt.AddDays(3));
      v.MoveTo(VacancyStage.Applied, MaturedAt.AddDays(4));
    });

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_MovedToInterviewingWithoutEvent_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, MaturedAt, afterApply: v => v.MoveTo(VacancyStage.Interviewing, MaturedAt.AddDays(1)));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_PositiveButNotMaturedApplication_IsExcludedFromPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var young = Now.AddDays(-3);
    var vacancyId = await ApplyAsync(trackId, young);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, young.AddDays(1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
    Assert.Equal(0, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PositiveAndNegativeApplications_ComputesRateAndWilsonInterval()
  {
    var trackId = await CreateStartedTrackAsync();
    var positive = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(positive, ApplicationEventType.Screening, MaturedAt.AddDays(1));
    await ApplyAsync(trackId, MaturedAt.AddDays(-1));
    await ApplyAsync(trackId, MaturedAt.AddDays(-2));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Positive);
    Assert.Equal(3, metrics.Conversion.Matured);
    Assert.Equal(1 / 3.0, metrics.Conversion.Rate!.Value, 10);
    Assert.Equal(WilsonInterval.Compute(1, 3), metrics.Conversion.Interval);
  }

  [Fact]
  public async Task GetAsync_MaturedBelowMinSampleSize_IsInsufficientData()
  {
    var trackId = await CreateStartedTrackAsync();
    await UpdateGoalAsync(minSampleSize: 2);
    await ApplyAsync(trackId, MaturedAt);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.True(metrics.Conversion.IsInsufficientData);
  }

  [Fact]
  public async Task GetAsync_MaturedEqualsMinSampleSize_IsSufficientData()
  {
    var trackId = await CreateStartedTrackAsync();
    await UpdateGoalAsync(minSampleSize: 2);
    await ApplyAsync(trackId, MaturedAt);
    await ApplyAsync(trackId, MaturedAt.AddDays(-1));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.False(metrics.Conversion.IsInsufficientData);
  }

  [Fact]
  public async Task GetAsync_NoPeriodStart_DefaultsToStartedAt()
  {
    var trackId = await CreateStartedTrackAsync();
    var startedAt = Now.AddDays(-30);
    await MutateTrackAsync(trackId, h => h.StartedAt = startedAt);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(startedAt, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_ApplicationBeforeStartedAt_IsExcludedFromConversion()
  {
    var trackId = await CreateStartedTrackAsync();
    await MutateTrackAsync(trackId, h => h.StartedAt = Now.AddDays(-30));
    await ApplyAsync(trackId, Now.AddDays(-40));
    await ApplyAsync(trackId, Now.AddDays(-25));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ApplicationExactlyAtPeriodStart_IsIncluded()
  {
    var trackId = await CreateStartedTrackAsync();
    var startedAt = Now.AddDays(-30);
    await MutateTrackAsync(trackId, h => h.StartedAt = startedAt);
    await ApplyAsync(trackId, startedAt);

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ExplicitPeriodStart_NarrowsConversionBeyondStartedAt()
  {
    var trackId = await CreateStartedTrackAsync();
    var periodStart = Now.AddDays(-25);
    await ApplyAsync(trackId, Now.AddDays(-30));
    await ApplyAsync(trackId, Now.AddDays(-20));

    var metrics = await Metrics.GetAsync(trackId, periodStart);

    Assert.Equal(periodStart, metrics.PeriodStart);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PeriodStartInsideWeek_NarrowsWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 9, 12));
    await ApplyAsync(trackId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(trackId, Utc(11, 10));

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_PeriodStartInsideWeek_NarrowsMarket()
  {
    var trackId = await CreateStartedTrackAsync();
    await CreateVacancyAsync(trackId, Utc(11, 9, 12));
    await CreateVacancyAsync(trackId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(trackId, Utc(11, 10));

    Assert.Equal(1, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_StartedAtWithinWeek_NarrowsWeeklyApplicationsByDefault()
  {
    var trackId = await CreateStartedTrackAsync();
    await MutateTrackAsync(trackId, h => h.StartedAt = Utc(11, 11));
    await ApplyAsync(trackId, Utc(11, 10, 12));
    await ApplyAsync(trackId, Utc(11, 11, 12));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_TrackWithoutStartedAt_FallsBackToCreatedAt()
  {
    var createdAt = Now.AddDays(-30);
    var trackId = await CreateTrackCreatedAtAsync(createdAt);
    await ApplyAsync(trackId, Now.AddDays(-40));
    await ApplyAsync(trackId, Now.AddDays(-25));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(createdAt, metrics.PeriodStart);
    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public async Task GetAsync_NonUtcPeriodStart_Throws(DateTimeKind kind)
  {
    var trackId = await CreateTrackAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(
      () => Metrics.GetAsync(trackId, DateTime.SpecifyKind(Now.AddDays(-5), kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public async Task GetAsync_NonUtcAsOf_Throws(DateTimeKind kind)
  {
    var trackId = await CreateTrackAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(
      () => Metrics.GetAsync(trackId, null, DateTime.SpecifyKind(Now, kind)));
  }

  [Fact]
  public async Task GetAsync_PeriodStartAfterAsOf_Throws()
  {
    var trackId = await CreateTrackAsync();

    await Assert.ThrowsAnyAsync<ArgumentException>(() => Metrics.GetAsync(trackId, Now.AddSeconds(1), Now));
  }

  [Fact]
  public async Task GetAsync_PeriodStartEqualToAsOf_DoesNotThrow()
  {
    var trackId = await CreateTrackAsync();

    var metrics = await Metrics.GetAsync(trackId, Now, Now);

    Assert.Equal(Now, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_AsOfInThePast_UsesThatWeek()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Now.AddDays(-7);

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(asOf, metrics.AsOf);
    Assert.Equal(Utc(11, 2), metrics.Week.Start);
    Assert.Equal(Utc(11, 9), metrics.Week.End);
  }

  [Fact]
  public async Task GetAsync_VacancyDiscoveredAfterAsOf_IsInvisibleInMarket()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Utc(11, 5, 10);
    await CreateVacancyAsync(trackId, Utc(11, 4, 12));
    await CreateVacancyAsync(trackId, asOf);
    await CreateVacancyAsync(trackId, asOf.AddSeconds(1));
    await CreateVacancyAsync(trackId, Utc(11, 6, 12));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(2, metrics.Market.VacanciesThisWeek);
  }

  [Fact]
  public async Task GetAsync_ApplicationAfterAsOf_IsInvisibleInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Utc(11, 5, 10);
    await ApplyAsync(trackId, Utc(11, 3, 12));
    await ApplyAsync(trackId, asOf.AddSeconds(1));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_AsOfInThePast_MaturityIsRelativeToAsOf()
  {
    var trackId = await CreateStartedTrackAsync();
    var asOf = Utc(11, 5, 10);
    await ApplyAsync(trackId, asOf.AddDays(-21));
    await ApplyAsync(trackId, asOf.AddDays(-11));
    await ApplyAsync(trackId, asOf.AddDays(2));

    var metrics = await Metrics.GetAsync(trackId, null, asOf);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_UnknownTrack_Throws()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Metrics.GetAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetAsync_TrackOfAnotherUser_Throws()
  {
    var otherUserId = await CreateProfileAsync();
    var foreignTrackId = await CreateTrackAsync(userId: otherUserId);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Metrics.GetAsync(foreignTrackId));
  }

  [Fact]
  public async Task GetAsync_OtherUsersData_DoesNotLeakIntoOwnMetrics()
  {
    var otherUserId = await CreateProfileAsync();
    var ownId = await CreateStartedTrackAsync();
    var foreignId = await CreateStartedTrackAsync(otherUserId);
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
  public async Task GetActivityAsync_ApplicationsOfAllTracks_AreSummedForTheWeek()
  {
    var firstId = await CreateStartedTrackAsync();
    var secondId = await CreateStartedTrackAsync(name: "Other");
    await ApplyAsync(firstId, Utc(11, 9, 9));
    await ApplyAsync(firstId, Utc(11, 10, 9));
    await ApplyAsync(secondId, Utc(11, 11, 9));

    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(3, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_ApplicationsOutsideWeekOrAfterAsOf_AreNotCounted()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, WeekStart.AddMinutes(-1));
    await ApplyAsync(trackId, Now.AddSeconds(1));
    await ApplyAsync(trackId, Now);

    var activity = await Metrics.GetActivityAsync();

    Assert.Equal(1, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_ExplicitAsOf_UsesThatWeek()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 3, 12));
    await ApplyAsync(trackId, Utc(11, 10, 12));

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
    var ownId = await CreateStartedTrackAsync();
    var foreignId = await CreateStartedTrackAsync(otherUserId);
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
    var trackId = await CreateStartedTrackAsync();

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(trackId, asOf: Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 16), metrics.Week.FirstDay);
    Assert.Equal(Utc(11, 15, 21), metrics.Week.Start);
    Assert.Equal(Utc(11, 22, 21), metrics.Week.End);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_ApplicationAfterLocalMondayMidnight_IsCountedInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 15, 21, 30));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(trackId, asOf: Utc(11, 15, 22));

    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_ApplicationOnLocalSunday_IsNotCountedInWeeklyApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 15, 20));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(trackId, asOf: Utc(11, 15, 22));

    Assert.Equal(0, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetAsync_UtcZoneSameInstants_CountsApplicationOnUtcSunday()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 15, 20));

    var metrics = await MetricsInZone("UTC").GetAsync(trackId, asOf: Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 9), metrics.Week.FirstDay);
    Assert.Equal(1, metrics.WeeklyApplications);
  }

  [Fact]
  public async Task GetActivityAsync_MoscowZone_CountsOnlyApplicationsSinceLocalMonday()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 15, 21, 30));
    await ApplyAsync(trackId, Utc(11, 15, 20));

    var activity = await MetricsInZone("Europe/Moscow").GetActivityAsync(Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 16), activity.Week.FirstDay);
    Assert.Equal(1, activity.Applications);
  }

  [Fact]
  public async Task GetActivityAsync_UtcZoneSameInstants_CountsBothApplications()
  {
    var trackId = await CreateStartedTrackAsync();
    await ApplyAsync(trackId, Utc(11, 15, 21, 30));
    await ApplyAsync(trackId, Utc(11, 15, 20));

    var activity = await MetricsInZone("UTC").GetActivityAsync(Utc(11, 15, 22));

    Assert.Equal(new DateOnly(2026, 11, 9), activity.Week.FirstDay);
    Assert.Equal(2, activity.Applications);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_PositiveEventAtLocalMidnightOfApplyDay_IsPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddHours(-3));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task GetAsync_MoscowZone_PositiveEventBeforeLocalApplyDay_IsNotPositive()
  {
    var trackId = await CreateStartedTrackAsync();
    var vacancyId = await ApplyAsync(trackId, MaturedAt);
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.Date.AddHours(-3).AddSeconds(-1));

    var metrics = await MetricsInZone("Europe/Moscow").GetAsync(trackId);

    Assert.Equal(0, metrics.Conversion.Positive);
  }
}
