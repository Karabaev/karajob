using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Users;

namespace Karajob.Application.WeeklyReviews;

public class WeeklyReviewService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  HypothesisMetricsService metricsService)
{
  public Task<WeeklyReviewSummary> GetAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
