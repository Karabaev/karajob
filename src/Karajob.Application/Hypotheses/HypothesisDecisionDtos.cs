using Karajob.Application.Metrics;
using Karajob.Domain.Hypotheses;

namespace Karajob.Application.Hypotheses;

public record CheckInInput(DateOnly Day, int Motivation, int Confidence, string? Note);

public record CheckInDetails(Guid Id, DateOnly Week, int Motivation, int Confidence, string? Note, int AverageTenths);

public record HypothesisReview(HypothesisMetrics Metrics, WellBeingSummary WellBeing, int WellBeingThreshold, DecisionHint Hint);

public record DraftOption(Guid Id, string Name);

public record DisableOptions(IReadOnlyList<DraftOption> Drafts, int ActiveCountAfter);

public record DecisionInput(HypothesisDecisionKind Kind, string? Reason, string? Changes, Guid? ActivateDraftId);

public record DecisionDetails(
  Guid Id,
  DateTime DecidedAt,
  HypothesisDecisionKind Kind,
  string? Reason,
  string? Changes,
  DateTime PeriodStartedAt,
  int MarketVacancies,
  int MarketThreshold,
  int PositiveApplications,
  int MaturedApplications,
  int ConversionThresholdPermille,
  int? WellBeingTenths,
  DecisionHint Hint);

public record DecisionResult(DecisionDetails Decision, HypothesisStatus Status, int ActiveCount, ActiveHypothesesStatus ActiveStatus);
