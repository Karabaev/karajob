using Karajob.Application.WeeklyReviews;
using Karajob.Domain.Applications;
using Karajob.Domain.Goals;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;

namespace Karajob.Tests.Application.WeeklyReviews;

public class WeeklyReviewServiceTests : HypothesisDecisionServiceFixture
{
  private WeeklyReviewService Review => ReviewFor(SeedData.OwnerId);

  private WeeklyReviewService ReviewFor(Guid userId, TimeZoneInfo? timeZone = null)
  {
    return TestDbContextFactory.CreateWeeklyReviewService(Database, userId, Clock, timeZone);
  }

  [Fact]
  public async Task GetAsync_NoData_ReturnsEmptyListsAndDefaultThreshold()
  {
    var summary = await Review.GetAsync();

    Assert.Empty(summary.Hypotheses);
    Assert.Empty(summary.StaleApplications);
    Assert.Equal(SearchGoalDefaults.MaturationDays, summary.StaleAfterDays);
  }

  [Fact]
  public async Task GetAsync_Always_ReturnsCurrentCalendarWeek()
  {
    var summary = await Review.GetAsync();

    Assert.Equal(CurrentWeek, summary.Week.FirstDay);
    Assert.Equal(WeekStart, summary.Week.Start);
  }

  [Fact]
  public async Task GetAsync_ProfileZoneAheadOfUtc_WeekFollowsZone()
  {
    Clock.UtcNow = Utc(11, 15, 20);
    var tokyo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    var inUtc = await Review.GetAsync();
    var inTokyo = await ReviewFor(SeedData.OwnerId, tokyo).GetAsync();

    Assert.Equal(CurrentWeek, inUtc.Week.FirstDay);
    Assert.Equal(CurrentWeek.AddDays(7), inTokyo.Week.FirstDay);
  }

  [Fact]
  public async Task GetAsync_WithApplications_ActivityMatchesMetricsService()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 7);
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Utc(11, 10, 12));
    await ApplyAsync(hypothesisId, Utc(11, 11, 12));

    var summary = await Review.GetAsync();

    Assert.Equal(await Metrics.GetActivityAsync(), summary.Activity);
    Assert.Equal(2, summary.Activity.Applications);
    Assert.Equal(7, summary.Activity.WeeklyTarget);
  }

  [Fact]
  public async Task GetAsync_MixedStatuses_ListsOnlyActiveAndExpanded()
  {
    var active = await CreateActiveAsync("Active one");
    var expanded = await CreateActiveAsync("Expanded one", status: HypothesisStatus.Expanded);
    await CreateActiveAsync("Paused one", status: HypothesisStatus.Paused);
    await CreateActiveAsync("Disabled one", status: HypothesisStatus.Disabled);
    await CreateHypothesisAsync("Draft one");

    var summary = await Review.GetAsync();

    Assert.Equal(2, summary.Hypotheses.Count);
    Assert.Contains(summary.Hypotheses, h => h.Id == active && h.Status == HypothesisStatus.Active);
    Assert.Contains(summary.Hypotheses, h => h.Id == expanded && h.Status == HypothesisStatus.Expanded);
  }

  [Fact]
  public async Task GetAsync_Hypotheses_AreSortedByNameIgnoringCase()
  {
    await CreateActiveAsync("beta");
    await CreateActiveAsync("Charlie");
    await CreateActiveAsync("Alpha");

    var summary = await Review.GetAsync();

    Assert.Equal(["Alpha", "beta", "Charlie"], summary.Hypotheses.Select(h => h.Name).ToArray());
  }

  [Fact]
  public async Task GetAsync_CheckInThisWeek_MarksCheckedIn()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));

    var summary = await Review.GetAsync();

    Assert.True(Assert.Single(summary.Hypotheses).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInSavedMidWeek_MarksCheckedIn()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 12)));

    var summary = await Review.GetAsync();

    Assert.True(Assert.Single(summary.Hypotheses).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInOnlyLastWeek_IsNotCheckedIn()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek));

    var summary = await Review.GetAsync();

    Assert.False(Assert.Single(summary.Hypotheses).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInOfAnotherHypothesis_DoesNotMarkThisOne()
  {
    var first = await CreateActiveAsync("First");
    await CreateActiveAsync("Second");
    await Decisions.SaveCheckInAsync(first, CheckIn(CurrentWeek));

    var summary = await Review.GetAsync();

    Assert.True(summary.Hypotheses.Single(h => h.Name == "First").CheckedInThisWeek);
    Assert.False(summary.Hypotheses.Single(h => h.Name == "Second").CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_AppliedBeyondThreshold_ListsStaleApplicationWithDetails()
  {
    var hypothesisId = await CreateActiveAsync("Gamedev EU");
    var companyId = await CreateCompanyAsync("pixel-forge");
    var appliedAt = Now.AddDays(-20);
    var vacancyId = await ApplyAsync(hypothesisId, appliedAt);
    await MutateAsync(vacancyId, v =>
    {
      v.CompanyId = companyId;
      v.RoleId = SeedData.UnityDeveloperRoleId;
      v.Title = "Senior Unity";
    });

    var summary = await Review.GetAsync();

    var stale = Assert.Single(summary.StaleApplications);
    Assert.Equal(vacancyId, stale.VacancyId);
    Assert.Equal("Senior Unity", stale.Title);
    Assert.Equal("pixel-forge", stale.CompanyName);
    Assert.Equal("Unity Developer", stale.RoleName);
    Assert.Equal(hypothesisId, stale.HypothesisId);
    Assert.Equal("Gamedev EU", stale.HypothesisName);
    Assert.Equal(appliedAt, stale.AppliedAt);
    Assert.Equal(appliedAt, stale.LastActivityAt);
    Assert.Equal(20, stale.IdleDays);
    Assert.False(string.IsNullOrWhiteSpace(stale.Url));
  }

  [Fact]
  public async Task GetAsync_VacancyWithoutCompanyAndRole_HasNullNames()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-20));

    var stale = Assert.Single((await Review.GetAsync()).StaleApplications);

    Assert.Null(stale.CompanyName);
    Assert.Null(stale.RoleName);
  }

  [Fact]
  public async Task GetAsync_ExactlyAtThreshold_IsListed()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-14));

    var stale = Assert.Single((await Review.GetAsync()).StaleApplications);

    Assert.Equal(14, stale.IdleDays);
  }

  [Fact]
  public async Task GetAsync_OneDayBelowThreshold_IsNotListed()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-13));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_RecentNote_ResetsIdleTime()
  {
    var hypothesisId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(hypothesisId, Now.AddDays(-30));
    await AddEventAsync(vacancyId, ApplicationEventType.Note, Now.AddDays(-2));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_RecentScreening_ResetsIdleTime()
  {
    var hypothesisId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(hypothesisId, Now.AddDays(-30));
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, Now.AddDays(-3));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_OldNoteAfterApply_LastActivityIsLatestEvent()
  {
    var hypothesisId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(hypothesisId, Now.AddDays(-40));
    var noteAt = Now.AddDays(-20);
    await AddEventAsync(vacancyId, ApplicationEventType.Note, noteAt);

    var stale = Assert.Single((await Review.GetAsync()).StaleApplications);

    Assert.Equal(noteAt, stale.LastActivityAt);
    Assert.Equal(20, stale.IdleDays);
    Assert.Equal(Now.AddDays(-40), stale.AppliedAt);
  }

  [Fact]
  public async Task GetAsync_InterviewingVacancy_IsNotListed()
  {
    var hypothesisId = await CreateActiveAsync();
    var appliedAt = Now.AddDays(-30);
    await ApplyAsync(hypothesisId, appliedAt, afterApply: v => v.MoveTo(VacancyStage.Interviewing, appliedAt));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_ClosedVacancy_IsNotListed()
  {
    var hypothesisId = await CreateActiveAsync();
    var appliedAt = Now.AddDays(-30);
    await ApplyAsync(hypothesisId, appliedAt, afterApply: v => v.Close(ApplicationOutcome.Ghosted, appliedAt));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_VacancyNotApplied_IsNotListed()
  {
    var hypothesisId = await CreateActiveAsync();
    await CreateVacancyAsync(hypothesisId, Now.AddDays(-30));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_StaleVacancyOfPausedHypothesis_IsStillListed()
  {
    var hypothesisId = await CreateActiveAsync("Paused one", status: HypothesisStatus.Paused);
    await ApplyAsync(hypothesisId, Now.AddDays(-20));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.Hypotheses);
    var stale = Assert.Single(summary.StaleApplications);
    Assert.Equal("Paused one", stale.HypothesisName);
  }

  [Fact]
  public async Task GetAsync_StaleApplications_AreSortedByIdleDaysDescending()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-20));
    await ApplyAsync(hypothesisId, Now.AddDays(-45));
    await ApplyAsync(hypothesisId, Now.AddDays(-30));

    var summary = await Review.GetAsync();

    Assert.Equal([45, 30, 20], summary.StaleApplications.Select(s => s.IdleDays).ToArray());
  }

  [Fact]
  public async Task GetAsync_ChangedMaturationDays_ThresholdFollowsGoal()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-8));
    Assert.Empty((await Review.GetAsync()).StaleApplications);

    await UpdateGoalAsync(maturationDays: 7);
    var summary = await Review.GetAsync();

    Assert.Equal(7, summary.StaleAfterDays);
    Assert.Equal(8, Assert.Single(summary.StaleApplications).IdleDays);
  }

  [Fact]
  public async Task GetAsync_AnotherProfileData_IsInvisible()
  {
    var otherId = await CreateProfileAsync();
    var otherHypothesisId = await CreateActiveAsync("Other profile hypothesis", otherId);
    await ApplyAsync(otherHypothesisId, Now.AddDays(-30), otherId);

    var summary = await Review.GetAsync();

    Assert.Empty(summary.Hypotheses);
    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_OwnerData_IsInvisibleToAnotherProfile()
  {
    var otherId = await CreateProfileAsync();
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-30));

    var summary = await ReviewFor(otherId).GetAsync();

    Assert.Empty(summary.Hypotheses);
    Assert.Empty(summary.StaleApplications);
  }
}
