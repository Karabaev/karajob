using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Resources;
using Karajob.Application.Users;
using Microsoft.Extensions.Localization;

namespace Karajob.Application.Hypotheses;

public class HypothesisDecisionService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  HypothesisMetricsService metricsService,
  IStringLocalizer<HypothesisMessages> localizer)
{
  public Task<CheckInDetails> SaveCheckInAsync(Guid hypothesisId, CheckInInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IReadOnlyList<CheckInDetails>> ListCheckInsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<HypothesisReview> GetReviewAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<DisableOptions> GetDisableOptionsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<DecisionResult> RecordDecisionAsync(Guid hypothesisId, DecisionInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IReadOnlyList<DecisionDetails>> ListDecisionsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
