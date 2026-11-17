using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Web.Components.Common;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Tests.Application.Tracks;
using Karajob.Web.Components.Tracks;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Tracks;

public class TrackCriteriaCardTests : TrackDecisionServiceFixture, IAsyncLifetime
{
  private static readonly DateTime MaturedAt = Now.AddDays(-20);
  private bool servicesRegistered;

  private BunitContext Context { get; } = new();

  [Fact]
  public async Task Render_WithoutApplications_ShowsInsufficientDataForConversion()
  {
    var trackId = await CreateStartedTrackAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-conversion").TextContent;
      Assert.Contains("insufficient data", text);
      Assert.Contains("0 of 0", text);
      Assert.DoesNotContain("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_SufficientSampleWithPositive_ShowsRateIntervalAndOnTrack()
  {
    await UpdateGoalAsync(minSampleSize: 4);
    var trackId = await CreateStartedTrackAsync();
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-conversion").TextContent;
      Assert.Contains("25,0 %", text);
      Assert.Contains("1 of 4", text);
      Assert.Contains("95% CI", text);
      Assert.Contains("on track", text);
    });
  }

  [Fact]
  public async Task Render_SufficientSampleWithoutPositive_ShowsBelowThreshold()
  {
    await UpdateGoalAsync(minSampleSize: 4);
    var trackId = await CreateStartedTrackAsync();
    await ApplyMaturedAsync(trackId, total: 5, positive: 0);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-conversion").TextContent;
      Assert.Contains("0,0 %", text);
      Assert.Contains("0 of 5", text);
      Assert.Contains("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_InsufficientSampleWithMaturedApplications_ShowsRateButNotOnTrack()
  {
    await UpdateGoalAsync(minSampleSize: 30);
    var trackId = await CreateStartedTrackAsync();
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-conversion").TextContent;
      Assert.Contains("25,0 %", text);
      Assert.Contains("1 of 4", text);
      Assert.Contains("insufficient data", text);
      Assert.DoesNotContain("on track", text);
    });
  }

  [Fact]
  public async Task Render_MarketBelowThreshold_ShowsCountAndBelowThreshold()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 5);
    var trackId = await CreateStartedTrackAsync();
    await CreateVacanciesAsync(trackId, 3);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-market").TextContent;
      Assert.Equal("3", cut.Find(".criteria-market-count").TextContent.Trim());
      Assert.Contains("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_MarketAtThreshold_ShowsOnTrack()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 3);
    var trackId = await CreateStartedTrackAsync();
    await CreateVacanciesAsync(trackId, 3);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-market").TextContent;
      Assert.Equal("3", cut.Find(".criteria-market-count").TextContent.Trim());
      Assert.Contains("on track", text);
      Assert.DoesNotContain("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_ActivityBelowTarget_ShowsProfileWideCountAndBelowThreshold()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 7);
    var trackId = await CreateStartedTrackAsync();
    var otherId = await CreateStartedTrackAsync(name: "Gamedev EU");
    await ApplyThisWeekAsync(trackId, 2);
    await ApplyThisWeekAsync(otherId, 3);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-activity").TextContent;
      Assert.Contains("for this track: 2", text);
      Assert.Contains("5 of 7", text);
      Assert.Contains("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_ActivityReachedByOtherTrack_ShowsOnTrack()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 4);
    var trackId = await CreateStartedTrackAsync();
    var otherId = await CreateStartedTrackAsync(name: "Gamedev EU");
    await ApplyThisWeekAsync(trackId, 1);
    await ApplyThisWeekAsync(otherId, 3);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-activity").TextContent;
      Assert.Contains("4 of 4", text);
      Assert.Contains("on track", text);
      Assert.DoesNotContain("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_ShowsOneVerdictChipPerRowWithVerdictLabel()
  {
    var trackId = await CreateStartedTrackAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      Assert.NotNull(cut.Find(".track-criteria"));
      foreach (var row in new[] { ".criteria-market", ".criteria-conversion", ".criteria-activity" })
      {
        var chips = cut.FindAll($"{row} .criteria-verdict");
        Assert.Single(chips);
        Assert.Contains(chips[0].TextContent.Trim(), new[] { "insufficient data", "below threshold", "on track" });
      }
    });
  }

  [Fact]
  public void Render_UnknownTrack_ShowsUnavailableMessageInsteadOfThrowing()
  {
    var cut = RenderCard(Guid.NewGuid());

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Metrics unavailable", cut.Find(".criteria-unavailable").TextContent);
      Assert.Empty(cut.FindAll(".criteria-market"));
    });
  }

  [Fact]
  public async Task Render_TrackIdChanged_ShowsMetricsOfNewTrack()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 5);
    var firstId = await CreateStartedTrackAsync();
    var secondId = await CreateStartedTrackAsync(name: "Gamedev EU");
    await CreateVacanciesAsync(firstId, 3);
    var cut = RenderCard(firstId);
    cut.WaitForAssertion(() => Assert.Equal("3", cut.Find(".criteria-market-count").TextContent.Trim()));

    cut.Render(p => p.Add(c => c.TrackId, secondId));

    cut.WaitForAssertion(() => Assert.Equal("0", cut.Find(".criteria-market-count").TextContent.Trim()));
  }

  [Fact]
  public async Task Render_NoCheckIns_ShowsDashAndNoLowSignal()
  {
    var trackId = await CreateActiveAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("—", cut.Find(".criteria-wellbeing").TextContent);
      Assert.Empty(cut.FindAll(".criteria-wellbeing-low"));
    });
  }

  [Fact]
  public async Task Render_CheckIns_ShowsLatestAverageWithOneDecimal()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 2, 3));

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      Assert.Matches(@"2[.,]5", cut.Find(".criteria-wellbeing").TextContent);
      Assert.DoesNotContain("—", cut.Find(".criteria-wellbeing").TextContent);
      Assert.Empty(cut.FindAll(".criteria-wellbeing-low"));
    });
  }

  [Fact]
  public async Task Render_TwoLowConsecutiveCheckIns_ShowsLowSignal()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 1, 2));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 2, 1));

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".criteria-wellbeing-low"));
      Assert.Matches(@"1[.,]5", cut.Find(".criteria-wellbeing").TextContent);
    });
  }

  [Fact]
  public async Task Render_NarrowMarket_ShowsExpandHintChip()
  {
    var trackId = await CreateActiveAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() =>
    {
      var chip = cut.Find(".criteria-hint");
      Assert.Equal(nameof(DecisionHint.Expand), chip.GetAttribute("data-hint"));
      Assert.False(string.IsNullOrWhiteSpace(chip.TextContent));
    });
  }

  [Fact]
  public async Task Render_LowWellBeing_ShowsDisableHintChipDifferentFromExpand()
  {
    var expandId = await CreateActiveAsync("Narrow market");
    var disableId = await CreateActiveAsync("Burnout");
    await Decisions.SaveCheckInAsync(disableId, CheckIn(PreviousWeek, 1, 1));
    await Decisions.SaveCheckInAsync(disableId, CheckIn(CurrentWeek, 1, 1));
    var expandCut = RenderCard(expandId);
    expandCut.WaitForAssertion(() => Assert.NotNull(expandCut.Find(".criteria-hint")));
    var expandText = expandCut.Find(".criteria-hint").TextContent.Trim();

    var disableCut = RenderCard(disableId);

    disableCut.WaitForAssertion(() =>
    {
      var chip = disableCut.Find(".criteria-hint");
      Assert.Equal(nameof(DecisionHint.Disable), chip.GetAttribute("data-hint"));
      Assert.NotEqual(expandText, chip.TextContent.Trim());
    });
  }

  [Fact]
  public async Task Render_BroadMarketWithoutMaturedApplications_ShowsNotEnoughDataHintChip()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await CreateVacanciesAsync(trackId, 1);

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() => Assert.Equal(nameof(DecisionHint.NotEnoughData), cut.Find(".criteria-hint").GetAttribute("data-hint")));
  }

  [Fact]
  public async Task Render_ActiveTrack_ShowsExactlyOneHintChip()
  {
    var trackId = await CreateActiveAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".criteria-hint")));
  }

  [Fact]
  public async Task Render_ShowActivityDefault_ShowsActivityBlock()
  {
    var trackId = await CreateStartedTrackAsync();

    var cut = RenderCard(trackId);

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".criteria-activity")));
  }

  [Fact]
  public async Task Render_ShowActivityFalse_HidesActivityBlockButKeepsOtherRows()
  {
    var trackId = await CreateStartedTrackAsync();

    var cut = RenderCard(trackId, showActivity: false);

    cut.WaitForAssertion(() =>
    {
      Assert.NotNull(cut.Find(".criteria-market"));
      Assert.NotNull(cut.Find(".criteria-conversion"));
      Assert.Empty(cut.FindAll(".criteria-activity"));
    });
  }

  async Task IAsyncLifetime.InitializeAsync()
  {
    await InitializeAsync();
  }

  async Task IAsyncLifetime.DisposeAsync()
  {
    await Context.DisposeAsync();
    await DisposeAsync();
  }

  private IRenderedComponent<TrackCriteriaCard> RenderCard(Guid trackId, bool showActivity = true)
  {
    // bUnit forbids registrations after the first render, and some tests render two cards.
    if (!servicesRegistered)
    {
      Context.Services.AddSingleton(Metrics);
      Context.Services.AddSingleton(Decisions);
      servicesRegistered = true;
    }

    return Context.Render<TrackCriteriaCard>(p => p.Add(c => c.TrackId, trackId).Add(c => c.ShowActivity, showActivity));
  }

  private async Task ApplyMaturedAsync(Guid trackId, int total, int positive)
  {
    for (var i = 0; i < total; i++)
    {
      var vacancyId = await ApplyAsync(trackId, MaturedAt);
      if (i < positive)
      {
        await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.AddDays(2));
      }
    }
  }

  private async Task ApplyThisWeekAsync(Guid trackId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await ApplyAsync(trackId, Utc(11, 10, 12));
    }
  }

  private async Task CreateVacanciesAsync(Guid trackId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await CreateVacancyAsync(trackId, Utc(11, 10, 9));
    }
  }

  public TrackCriteriaCardTests()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddScoped<UserDates>();
  }
}
