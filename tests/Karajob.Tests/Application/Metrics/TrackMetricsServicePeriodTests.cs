using Karajob.Domain.Tracks;
using Karajob.Tests.Application.Tracks;

namespace Karajob.Tests.Application.Metrics;

public class TrackMetricsServicePeriodTests : TrackDecisionServiceFixture
{
  [Fact]
  public async Task GetAsync_PeriodStartedAtSet_UsesItInsteadOfStartedAt()
  {
    var trackId = await CreateActiveAsync();
    var expandedAt = Now.AddDays(-30);
    await MutateTrackAsync(trackId, h => h.ApplyDecision(TrackDecisionKind.Expand, expandedAt));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(expandedAt, metrics.PeriodStart);
  }

  [Fact]
  public async Task GetAsync_PeriodStartedAtSet_DoesNotCountApplicationsBeforeIt()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-40));
    await ApplyAsync(trackId, Now.AddDays(-20));
    await MutateTrackAsync(trackId, h => h.ApplyDecision(TrackDecisionKind.Expand, Now.AddDays(-30)));

    var metrics = await Metrics.GetAsync(trackId);

    Assert.Equal(1, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_ExplicitPeriodStart_WinsOverPeriodStartedAt()
  {
    var trackId = await CreateActiveAsync();
    await ApplyAsync(trackId, Now.AddDays(-40));
    await ApplyAsync(trackId, Now.AddDays(-20));
    await MutateTrackAsync(trackId, h => h.ApplyDecision(TrackDecisionKind.Expand, Now.AddDays(-30)));

    var metrics = await Metrics.GetAsync(trackId, Now.AddDays(-50));

    Assert.Equal(Now.AddDays(-50), metrics.PeriodStart);
    Assert.Equal(2, metrics.Conversion.Matured);
  }

  [Fact]
  public async Task GetAsync_PeriodStartedAtAfterStartedAt_ExpandedTrackReportsLaterStart()
  {
    var trackId = await CreateActiveAsync();
    var before = await Metrics.GetAsync(trackId);
    await MutateTrackAsync(trackId, h => h.ApplyDecision(TrackDecisionKind.Expand, Now));

    var after = await Metrics.GetAsync(trackId);

    Assert.Equal(Now.AddDays(-60), before.PeriodStart);
    Assert.Equal(Now, after.PeriodStart);
  }
}
