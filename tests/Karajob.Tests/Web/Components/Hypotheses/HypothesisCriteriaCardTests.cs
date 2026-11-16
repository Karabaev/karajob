using Bunit;
using Karajob.Domain.Applications;
using Karajob.Tests.Application.Metrics;
using Karajob.Web.Components.Hypotheses;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisCriteriaCardTests : HypothesisMetricsServiceFixture, IAsyncLifetime
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
      Assert.Contains("данных мало", text);
      Assert.Contains("0 из 0", text);
      Assert.DoesNotContain("ниже порога", text);
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
      Assert.Contains("1 из 4", text);
      Assert.Contains("95% ДИ", text);
      Assert.Contains("в норме", text);
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
      Assert.Contains("0 из 5", text);
      Assert.Contains("ниже порога", text);
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
      Assert.Contains("1 из 4", text);
      Assert.Contains("данных мало", text);
      Assert.DoesNotContain("в норме", text);
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
      Assert.Contains("ниже порога", text);
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
      Assert.Contains("в норме", text);
      Assert.DoesNotContain("ниже порога", text);
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
      Assert.Contains("по этой гипотезе: 2", text);
      Assert.Contains("5 из 7", text);
      Assert.Contains("ниже порога", text);
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
      Assert.Contains("4 из 4", text);
      Assert.Contains("в норме", text);
      Assert.DoesNotContain("ниже порога", text);
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
        Assert.Contains(chips[0].TextContent.Trim(), new[] { "данных мало", "ниже порога", "в норме" });
      }
    });
  }

  [Fact]
  public void Render_UnknownHypothesis_ShowsUnavailableMessageInsteadOfThrowing()
  {
    var cut = RenderCard(Guid.NewGuid());

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Метрики недоступны", cut.Find(".criteria-unavailable").TextContent);
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
  }
}
