using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Web.Components.Common;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Web.Components.Hypotheses;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisCriteriaCardTests : HypothesisDecisionServiceFixture, IAsyncLifetime
{
  private static readonly DateTime MaturedAt = Now.AddDays(-20);

  private BunitContext Context { get; } = new();

  [Fact]
  public async Task Render_WithoutApplications_ShowsInsufficientDataForConversion()
  {
    var hypothesisId = await CreateStartedHypothesisAsync();

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyMaturedAsync(hypothesisId, total: 5, positive: 0);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    await CreateVacanciesAsync(hypothesisId, 3);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    await CreateVacanciesAsync(hypothesisId, 3);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();
    var otherId = await CreateStartedHypothesisAsync(name: "Gamedev EU");
    await ApplyThisWeekAsync(hypothesisId, 2);
    await ApplyThisWeekAsync(otherId, 3);

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".criteria-activity").TextContent;
      Assert.Contains("for this hypothesis: 2", text);
      Assert.Contains("5 of 7", text);
      Assert.Contains("below threshold", text);
    });
  }

  [Fact]
  public async Task Render_ActivityReachedByOtherHypothesis_ShowsOnTrack()
  {
    await UpdateGoalAsync(weeklyApplicationTarget: 4);
    var hypothesisId = await CreateStartedHypothesisAsync();
    var otherId = await CreateStartedHypothesisAsync(name: "Gamedev EU");
    await ApplyThisWeekAsync(hypothesisId, 1);
    await ApplyThisWeekAsync(otherId, 3);

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateStartedHypothesisAsync();

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() =>
    {
      Assert.NotNull(cut.Find(".hypothesis-criteria"));
      foreach (var row in new[] { ".criteria-market", ".criteria-conversion", ".criteria-activity" })
      {
        var chips = cut.FindAll($"{row} .criteria-verdict");
        Assert.Single(chips);
        Assert.Contains(chips[0].TextContent.Trim(), new[] { "insufficient data", "below threshold", "on track" });
      }
    });
  }

  [Fact]
  public void Render_UnknownHypothesis_ShowsUnavailableMessageInsteadOfThrowing()
  {
    var cut = RenderCard(Guid.NewGuid());

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Metrics unavailable", cut.Find(".criteria-unavailable").TextContent);
      Assert.Empty(cut.FindAll(".criteria-market"));
    });
  }

  [Fact]
  public async Task Render_HypothesisIdChanged_ShowsMetricsOfNewHypothesis()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 5);
    var firstId = await CreateStartedHypothesisAsync();
    var secondId = await CreateStartedHypothesisAsync(name: "Gamedev EU");
    await CreateVacanciesAsync(firstId, 3);
    var cut = RenderCard(firstId);
    cut.WaitForAssertion(() => Assert.Equal("3", cut.Find(".criteria-market-count").TextContent.Trim()));

    cut.Render(p => p.Add(c => c.HypothesisId, secondId));

    cut.WaitForAssertion(() => Assert.Equal("0", cut.Find(".criteria-market-count").TextContent.Trim()));
  }

  [Fact]
  public async Task Render_NoCheckIns_ShowsDashAndNoLowSignal()
  {
    var hypothesisId = await CreateActiveAsync();

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("—", cut.Find(".criteria-wellbeing").TextContent);
      Assert.Empty(cut.FindAll(".criteria-wellbeing-low"));
    });
  }

  [Fact]
  public async Task Render_CheckIns_ShowsLatestAverageWithOneDecimal()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 2, 3));

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 1, 2));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 2, 1));

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".criteria-wellbeing-low"));
      Assert.Matches(@"1[.,]5", cut.Find(".criteria-wellbeing").TextContent);
    });
  }

  [Fact]
  public async Task Render_NarrowMarket_ShowsExpandHintChip()
  {
    var hypothesisId = await CreateActiveAsync();

    var cut = RenderCard(hypothesisId);

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
    var hypothesisId = await CreateActiveAsync();
    await CreateVacanciesAsync(hypothesisId, 1);

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() => Assert.Equal(nameof(DecisionHint.NotEnoughData), cut.Find(".criteria-hint").GetAttribute("data-hint")));
  }

  [Fact]
  public async Task Render_ActiveHypothesis_ShowsExactlyOneHintChip()
  {
    var hypothesisId = await CreateActiveAsync();

    var cut = RenderCard(hypothesisId);

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".criteria-hint")));
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

  private IRenderedComponent<HypothesisCriteriaCard> RenderCard(Guid hypothesisId)
  {
    Context.Services.AddSingleton(Metrics);
    Context.Services.AddSingleton(Decisions);
    return Context.Render<HypothesisCriteriaCard>(p => p.Add(c => c.HypothesisId, hypothesisId));
  }

  private async Task ApplyMaturedAsync(Guid hypothesisId, int total, int positive)
  {
    for (var i = 0; i < total; i++)
    {
      var vacancyId = await ApplyAsync(hypothesisId, MaturedAt);
      if (i < positive)
      {
        await AddEventAsync(vacancyId, ApplicationEventType.Screening, MaturedAt.AddDays(2));
      }
    }
  }

  private async Task ApplyThisWeekAsync(Guid hypothesisId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await ApplyAsync(hypothesisId, Utc(11, 10, 12));
    }
  }

  private async Task CreateVacanciesAsync(Guid hypothesisId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await CreateVacancyAsync(hypothesisId, Utc(11, 10, 9));
    }
  }

  public HypothesisCriteriaCardTests()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddScoped<UserDates>();
  }
}
