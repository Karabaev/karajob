using Bunit;
using Karajob.Application.Hypotheses;
using Karajob.Application.Users;
using Karajob.Application.WeeklyReviews;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Web.Components.Common;
using Karajob.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Pages;

public class WeeklyReviewPageTests : HypothesisDecisionServiceFixture, IAsyncLifetime
{
  private BunitContext Context { get; } = new();

  [Fact]
  public async Task Render_ActiveAndDraftHypotheses_ShowsCardOnlyForActive()
  {
    await CreateActiveAsync("Gamedev EU");
    await CreateActiveAsync("Unity RU", status: HypothesisStatus.Expanded);
    await CreateHypothesisAsync("Just a draft");

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var cards = cut.FindAll(".weekly-hypothesis");
      Assert.Equal(2, cards.Count);
      Assert.Contains(cards, c => c.TextContent.Contains("Gamedev EU"));
      Assert.Contains(cards, c => c.TextContent.Contains("Unity RU"));
      Assert.DoesNotContain(cards, c => c.TextContent.Contains("Just a draft"));
    });
  }

  [Fact]
  public async Task Render_ActiveHypothesis_ShowsCheckInAndDecisionButtonsWithoutActivityBlock()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".weekly-checkin-button"));
      Assert.Single(cut.FindAll(".weekly-decision-button"));
      Assert.NotEmpty(cut.FindAll(".hypothesis-criteria"));
    });
    Assert.Empty(cut.FindAll(".criteria-activity"));
  }

  [Fact]
  public async Task Render_NoCheckInThisWeek_HidesCheckedInChip()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForElement(".weekly-hypothesis");
    Assert.Empty(cut.FindAll(".weekly-checked-in"));
  }

  [Fact]
  public async Task Render_CheckInThisWeek_ShowsCheckedInChipOnlyOnThatHypothesis()
  {
    var checkedIn = await CreateActiveAsync("Checked one");
    await CreateActiveAsync("Other one");
    await Decisions.SaveCheckInAsync(checkedIn, CheckIn(CurrentWeek));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var chip = Assert.Single(cut.FindAll(".weekly-checked-in"));
      Assert.NotNull(chip);
      var cards = cut.FindAll(".weekly-hypothesis");
      Assert.Equal(2, cards.Count);
      Assert.Single(cards, c => c.QuerySelector(".weekly-checked-in") is not null && c.TextContent.Contains("Checked one"));
    });
  }

  [Fact]
  public async Task Render_CheckInOnlyLastWeek_HidesCheckedInChip()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek));

    var cut = RenderPage();

    cut.WaitForElement(".weekly-hypothesis");
    Assert.Empty(cut.FindAll(".weekly-checked-in"));
  }

  [Fact]
  public async Task Render_WeeklyGoal_ShowsApplicationsAndTarget()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 10);
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Utc(11, 10, 12));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".weekly-goal").TextContent;
      Assert.Contains("1", text);
      Assert.Contains("10", text);
    });
  }

  [Fact]
  public async Task Render_StaleApplication_ShowsRowWithGhostedButton()
  {
    var hypothesisId = await CreateActiveAsync("Gamedev EU");
    await ApplyAsync(hypothesisId, Now.AddDays(-20));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var row = Assert.Single(cut.FindAll(".stale-application"));
      Assert.Contains("Gamedev EU", row.TextContent);
      Assert.Contains("20", row.TextContent);
      Assert.NotNull(row.QuerySelector(".stale-ghosted"));
    });
    Assert.Empty(cut.FindAll(".stale-empty"));
  }

  [Fact]
  public async Task Render_FreshApplication_ShowsNoStaleRow()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-3));

    var cut = RenderPage();

    cut.WaitForElement(".stale-empty");
    Assert.Empty(cut.FindAll(".stale-application"));
  }

  [Fact]
  public async Task ClickGhosted_StaleApplication_ClosesVacancyAsGhostedAndRemovesRow()
  {
    var hypothesisId = await CreateActiveAsync();
    var vacancyId = await ApplyAsync(hypothesisId, Now.AddDays(-20));
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
    var hypothesisId = await CreateActiveAsync();
    var oldest = await ApplyAsync(hypothesisId, Now.AddDays(-40));
    var other = await ApplyAsync(hypothesisId, Now.AddDays(-20));
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".stale-application").Count));

    cut.FindAll(".stale-application")[0].QuerySelector(".stale-ghosted")!.Click();

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stale-application")));
    Assert.Equal(VacancyStage.Closed, (await LoadAsync(oldest)).Stage);
    Assert.Equal(VacancyStage.Applied, (await LoadAsync(other)).Stage);
  }

  [Fact]
  public void Render_NoStaleApplications_ShowsEmptyState()
  {
    var cut = RenderPage();

    cut.WaitForElement(".stale-empty");
    Assert.Empty(cut.FindAll(".stale-application"));
  }

  [Fact]
  public void Render_NoActiveHypotheses_ShowsEmptyState()
  {
    var cut = RenderPage();

    cut.WaitForElement(".weekly-hypotheses-empty");
    Assert.Empty(cut.FindAll(".weekly-hypothesis"));
  }

  [Fact]
  public async Task Render_ActiveHypothesis_HidesHypothesesEmptyState()
  {
    await CreateActiveAsync();

    var cut = RenderPage();

    cut.WaitForElement(".weekly-hypothesis");
    Assert.Empty(cut.FindAll(".weekly-hypotheses-empty"));
  }

  async Task IAsyncLifetime.InitializeAsync()
  {
    await InitializeAsync();
    var review = TestDbContextFactory.CreateWeeklyReviewService(Database, SeedData.OwnerId, Clock);
    Context.Services.AddSingleton(review);
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Metrics);
    Context.Services.AddSingleton(Decisions);
    Context.Services.AddSingleton(Hypotheses);
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
