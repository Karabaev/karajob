using Karajob.Domain.Hypotheses;
using Karajob.Tests.Application.Hypotheses;

namespace Karajob.Tests.Application.Metrics;

public class HypothesisMetricsServicePeriodTests : HypothesisDecisionServiceFixture
{
  [Fact]
  public async Task GetAsync_PeriodStartedAtSet_UsesItInsteadOfStartedAt()
  {
    var hypothesisId = await CreateActiveAsync();
    var expandedAt = Now.AddDays(-30);
    await MutateHypothesisAsync(hypothesisId, h => h.ApplyDecision(HypothesisDecisionKind.Expand, expandedAt));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(expandedAt, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_PeriodStartedAtSet_DoesNotCountApplicationsBeforeIt()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-40));
    await ApplyAsync(hypothesisId, Now.AddDays(-20));
    await MutateHypothesisAsync(hypothesisId, h => h.ApplyDecision(HypothesisDecisionKind.Expand, Now.AddDays(-30)));

    var metrics = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ExplicitPeriodStart_WinsOverPeriodStartedAt()
  {
    var hypothesisId = await CreateActiveAsync();
    await ApplyAsync(hypothesisId, Now.AddDays(-40));
    await ApplyAsync(hypothesisId, Now.AddDays(-20));
    await MutateHypothesisAsync(hypothesisId, h => h.ApplyDecision(HypothesisDecisionKind.Expand, Now.AddDays(-30)));

    var metrics = await Metrics.GetAsync(hypothesisId, Now.AddDays(-50));

    Assert.Equal(Now.AddDays(-50), metrics.PeriodStart);
    Assert.Equal(2, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PeriodStartedAtAfterStartedAt_ExpandedHypothesisReportsLaterStart()
  {
    var hypothesisId = await CreateActiveAsync();
    var before = await Metrics.GetAsync(hypothesisId);
    await MutateHypothesisAsync(hypothesisId, h => h.ApplyDecision(HypothesisDecisionKind.Expand, Now));

    var after = await Metrics.GetAsync(hypothesisId);

    Assert.Equal(Now.AddDays(-60), before.PeriodStart);
    Assert.Equal(Now, after.PeriodStart);
  }
}
