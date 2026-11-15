using Karajob.Application.Persistence;

namespace Karajob.Application.Metrics;

public class HypothesisMetricsService(IKarajobDbContextFactory dbContextFactory, TimeProvider timeProvider)
{
  public Task<HypothesisMetrics> GetAsync(
    Guid hypothesisId,
    DateTime? periodStart = null,
    DateTime? asOf = null,
    CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<ProfileActivity> GetActivityAsync(DateTime? asOf = null, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
