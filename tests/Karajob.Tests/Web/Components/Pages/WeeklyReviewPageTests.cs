using Bunit;
using Karajob.Application.Tracks;
using Karajob.Application.Users;
using Karajob.Application.WeeklyReviews;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Tracks;
using Karajob.Web.Components.Common;
using Karajob.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Pages;

public class WeeklyReviewPageTests : TrackDecisionServiceFixture, IAsyncLifetime
{
  private BunitContext Context { get; } = new();

  [Fact]
  public async Task Render_ActiveAndDraftTracks_ShowsCardOnlyForActive()
  {
    await CreateActiveAsync("Gamedev EU");
    await CreateActiveAsync("Unity RU", status: TrackStatus.Expanded);
    await CreateTrackAsync("Just a draft");

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var cards = cut.FindAll(".weekly-track");
      Assert.Equal(2, cards.Count);
      Assert.Contains(cards, c => c.TextContent.Contains("Gamedev EU"));
      Assert.Contains(cards, c => c.TextContent.Contains("Unity RU"));
      Assert.DoesNotContain(cards, c => c.TextContent.Contains("Just a draft"));
    });
  }

  [Fact]
  public async Task Render_ActiveTrack_ShowsCheckInAndDecisionButtonsWithoutActivityBlock()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".weekly-checkin-button"));
      Assert.Single(cut.FindAll(".weekly-decision-button"));
      Assert.NotEmpty(cut.FindAll(".criteria-market"));
    });
    Assert.Empty(cut.FindAll(".criteria-activity"));
  }

  [Fact]
  public async Task Render_NoCheckInThisWeek_HidesCheckedInChip()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForElement(".weekly-track");
    Assert.Empty(cut.FindAll(".weekly-checked-in"));
  }

  [Fact]
  public async Task Render_CheckInThisWeek_ShowsCheckedInChipOnlyOnThatTrack()
  {
    var checkedIn = await CreateActiveAsync("Checked one");
    await CreateActiveAsync("Other one");
    await Decisions.SaveCheckInAsync(checkedIn, CheckIn(CurrentWeek));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var chip = Assert.Single(cut.FindAll(".weekly-checked-in"));
      Assert.NotNull(chip);
      var cards = cut.FindAll(".weekly-track");
      Assert.Equal(2, cards.Count);
      Assert.Single(cards, c => c.QuerySelector(".weekly-checked-in") is not null && c.TextContent.Contains("Checked one"));
    });
  }

  [Fact]
  public async Task Render_CheckInOnlyLastWeek_HidesCheckedInChip()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek));

    var cut = RenderPage();

    cut.WaitForElement(".weekly-track");
    Assert.Empty(cut.FindAll(".weekly-checked-in"));
  }

  [Fact]
  public async Task Render_WeeklyGoal_ShowsApplicationsAndTarget()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 10);
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Utc(11, 10, 12));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".weekly-goal").TextContent;
      Assert.Contains("1 of 10", text);
    });
  }

  [Fact]
  public async Task Render_StaleApplication_ShowsRowWithGhostedButton()
  {
    var trackId = await CreateActiveAsync("Gamedev EU");
    await ApplyAsync(trackId, Now.AddDays(-20));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var row = Assert.Single(cut.FindAll(".stale-application"));
      Assert.Contains("Gamedev EU", row.TextContent);
      Assert.Contains("20 days without events", row.TextContent);
      Assert.NotNull(row.QuerySelector(".stale-ghosted"));
    });
    Assert.Empty(cut.FindAll(".stale-empty"));
  }

  [Fact]
  public async Task Render_FreshApplication_ShowsNoStaleRow()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-3));

    var cut = RenderPage();

    cut.WaitForElement(".stale-empty");
    Assert.Empty(cut.FindAll(".stale-application"));
  }

  [Fact]
  public async Task ClickGhosted_StaleApplication_ClosesVacancyAsGhostedAndRemovesRow()
  {
    var trackId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(trackId, Now.AddDays(-20));
    var cut = RenderPage();
    cut.WaitForElement(".stale-application .stale-ghosted").Click();

    cut.WaitForAssertion(() =>
    {
      Assert.Empty(cut.FindAll(".stale-application"));
      Assert.NotEmpty(cut.FindAll(".stale-empty"));
    });
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.Equal(ApplicationOutcome.Ghosted, vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task ClickGhosted_OneOfTwoStaleApplications_KeepsTheOtherRow()
  {
    var trackId = await CreateActiveAsync();
    var oldest = await ApplyAsync(trackId, Now.AddDays(-40));
    var other = await ApplyAsync(trackId, Now.AddDays(-20));
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".stale-application").Count));

    cut.FindAll(".stale-application")[0].QuerySelector(".stale-ghosted")!.Click();

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stale-application")));
    Assert.Equal(VacancyStage.Closed, (await LoadAsync(oldest)).Stage);
    Assert.Equal(VacancyStage.Applied, (await LoadAsync(other)).Stage);
  }

  [Fact]
  public async Task ClickGhosted_VacancyMovedInAnotherTab_KeepsStageAndDropsRow()
  {
    var trackId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(trackId, Now.AddDays(-20));
    var cut = RenderPage();
    var ghosted = cut.WaitForElement(".stale-application .stale-ghosted");
    await Service.MoveAsync(vacancyId, VacancyStage.Applied, VacancyStage.Interviewing);

    ghosted.Click();

    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".stale-application")));
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Interviewing, vacancy.Stage);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Fact]
  public void Render_NoStaleApplications_ShowsEmptyState()
  {
    var cut = RenderPage();

    cut.WaitForElement(".stale-empty");
    Assert.Empty(cut.FindAll(".stale-application"));
  }

  [Fact]
  public void Render_NoActiveTracks_ShowsEmptyState()
  {
    var cut = RenderPage();

    cut.WaitForElement(".weekly-tracks-empty");
    Assert.Empty(cut.FindAll(".weekly-track"));
  }

  [Fact]
  public async Task Render_ActiveTrack_HidesTracksEmptyState()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForElement(".weekly-track");
    Assert.Empty(cut.FindAll(".weekly-tracks-empty"));
  }

  async Task IAsyncLifetime.InitializeAsync()
  {
    await InitializeAsync();
    var review = TestDbContextFactory.CreateWeeklyReviewService(Database, SeedData.OwnerId, Clock);
    Context.Services.AddSingleton(review);
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Metrics);
    Context.Services.AddSingleton(Decisions);
    Context.Services.AddSingleton(Tracks);
  }

  async Task IAsyncLifetime.DisposeAsync()
  {
    await Context.DisposeAsync();
    await DisposeAsync();
  }

  private IRenderedComponent<WeeklyReview> RenderPage()
  {
    return Context.Render<WeeklyReview>();
  }

  public WeeklyReviewPageTests()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton<TimeProvider>(Clock);
    Context.Services.AddScoped<UserDates>();
  }
}
