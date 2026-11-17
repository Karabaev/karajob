using Karajob.Application.WeeklyReviews;
using Karajob.Domain.Applications;
using Karajob.Domain.Goals;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Tracks;

namespace Karajob.Tests.Application.WeeklyReviews;

public class WeeklyReviewServiceTests : TrackDecisionServiceFixture
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

    Assert.Empty(summary.Tracks);
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
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Utc(11, 10, 12));
    await ApplyAsync(trackId, Utc(11, 11, 12));

    var summary = await Review.GetAsync();

    Assert.Equal(await Metrics.GetActivityAsync(), summary.Activity);
    Assert.Equal(2, summary.Activity.Applications);
    Assert.Equal(7, summary.Activity.WeeklyTarget);
  }

  [Fact]
  public async Task GetAsync_MixedStatuses_ListsOnlyActiveAndExpanded()
  {
    var active = await CreateActiveAsync("Active one");
    var expanded = await CreateActiveAsync("Expanded one", status: TrackStatus.Expanded);
    await CreateActiveAsync("Paused one", status: TrackStatus.Paused);
    await CreateActiveAsync("Disabled one", status: TrackStatus.Disabled);
    await CreateTrackAsync("Draft one");

    var summary = await Review.GetAsync();

    Assert.Equal(2, summary.Tracks.Count);
    Assert.Contains(summary.Tracks, h => h.Id == active && h.Status == TrackStatus.Active);
    Assert.Contains(summary.Tracks, h => h.Id == expanded && h.Status == TrackStatus.Expanded);
  }

  [Fact]
  public async Task GetAsync_Tracks_AreSortedByNameIgnoringCase()
  {
    await CreateActiveAsync("beta");
    await CreateActiveAsync("Charlie");
    await CreateActiveAsync("Alpha");

    var summary = await Review.GetAsync();

    Assert.Equal(["Alpha", "beta", "Charlie"], summary.Tracks.Select(h => h.Name).ToArray());
  }

  [Fact]
  public async Task GetAsync_CheckInThisWeek_MarksCheckedIn()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));

    var summary = await Review.GetAsync();

    Assert.True(Assert.Single(summary.Tracks).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInSavedMidWeek_MarksCheckedIn()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 12)));

    var summary = await Review.GetAsync();

    Assert.True(Assert.Single(summary.Tracks).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInOnlyLastWeek_IsNotCheckedIn()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek));

    var summary = await Review.GetAsync();

    Assert.False(Assert.Single(summary.Tracks).CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_CheckInOfAnotherTrack_DoesNotMarkThisOne()
  {
    var first = await CreateActiveAsync("First");
    await CreateActiveAsync("Second");
    await Decisions.SaveCheckInAsync(first, CheckIn(CurrentWeek));

    var summary = await Review.GetAsync();

    Assert.True(summary.Tracks.Single(h => h.Name == "First").CheckedInThisWeek);
    Assert.False(summary.Tracks.Single(h => h.Name == "Second").CheckedInThisWeek);
  }

  [Fact]
  public async Task GetAsync_AppliedBeyondThreshold_ListsStaleApplicationWithDetails()
  {
    var trackId = await CreateActiveAsync("Gamedev EU");
    var companyId = await CreateCompanyAsync("pixel-forge");
    var appliedAt = Now.AddDays(-20);
    var vacancyId = await ApplyAsync(trackId, appliedAt);
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
    Assert.Equal(trackId, stale.TrackId);
    Assert.Equal("Gamedev EU", stale.TrackName);
    Assert.Equal(appliedAt, stale.AppliedAt);
    Assert.Equal(appliedAt, stale.LastActivityAt);
    Assert.Equal(20, stale.IdleDays);
    Assert.False(string.IsNullOrWhiteSpace(stale.Url));
  }

  [Fact]
  public async Task GetAsync_VacancyWithoutCompanyAndRole_HasNullNames()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-20));

    var stale = Assert.Single((await Review.GetAsync()).StaleApplications);

    Assert.Null(stale.CompanyName);
    Assert.Null(stale.RoleName);
  }

  [Fact]
  public async Task GetAsync_ExactlyAtThreshold_IsListed()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-14));

    var stale = Assert.Single((await Review.GetAsync()).StaleApplications);

    Assert.Equal(14, stale.IdleDays);
  }

  [Fact]
  public async Task GetAsync_OneDayBelowThreshold_IsNotListed()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-13));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_RecentNote_ResetsIdleTime()
  {
    var trackId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(trackId, Now.AddDays(-30));
    await AddEventAsync(vacancyId, ApplicationEventType.Note, Now.AddDays(-2));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_RecentScreening_ResetsIdleTime()
  {
    var trackId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(trackId, Now.AddDays(-30));
    await AddEventAsync(vacancyId, ApplicationEventType.Screening, Now.AddDays(-3));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_OldNoteAfterApply_LastActivityIsLatestEvent()
  {
    var trackId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(trackId, Now.AddDays(-40));
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
    var trackId = await CreateActiveAsync();
    var appliedAt = Now.AddDays(-30);
    await ApplyAsync(trackId, appliedAt, afterApply: v => v.MoveTo(VacancyStage.Interviewing, appliedAt));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_ClosedVacancy_IsNotListed()
  {
    var trackId = await CreateActiveAsync();
    var appliedAt = Now.AddDays(-30);
    await ApplyAsync(trackId, appliedAt, afterApply: v => v.Close(ApplicationOutcome.Ghosted, appliedAt));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_VacancyNotApplied_IsNotListed()
  {
    var trackId = await CreateActiveAsync();
    await CreateVacancyAsync(trackId, Now.AddDays(-30));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_StaleVacancyOfPausedTrack_IsStillListed()
  {
    var trackId = await CreateActiveAsync("Paused one", status: TrackStatus.Paused);
    await ApplyAsync(trackId, Now.AddDays(-20));

    var summary = await Review.GetAsync();

    Assert.Empty(summary.Tracks);
    var stale = Assert.Single(summary.StaleApplications);
    Assert.Equal("Paused one", stale.TrackName);
  }

  [Fact]
  public async Task GetAsync_StaleApplications_AreSortedByIdleDaysDescending()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-20));
    await ApplyAsync(trackId, Now.AddDays(-45));
    await ApplyAsync(trackId, Now.AddDays(-30));

    var summary = await Review.GetAsync();

    Assert.Equal([45, 30, 20], summary.StaleApplications.Select(s => s.IdleDays).ToArray());
  }

  [Fact]
  public async Task GetAsync_ChangedMaturationDays_ThresholdFollowsGoal()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-8));
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
    var otherTrackId = await CreateActiveAsync("Other profile track", otherId);
    await ApplyAsync(otherTrackId, Now.AddDays(-30), otherId);

    var summary = await Review.GetAsync();

    Assert.Empty(summary.Tracks);
    Assert.Empty(summary.StaleApplications);
  }

  [Fact]
  public async Task GetAsync_OwnerData_IsInvisibleToAnotherProfile()
  {
    var otherId = await CreateProfileAsync();
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-30));

    var summary = await ReviewFor(otherId).GetAsync();

    Assert.Empty(summary.Tracks);
    Assert.Empty(summary.StaleApplications);
  }
}
