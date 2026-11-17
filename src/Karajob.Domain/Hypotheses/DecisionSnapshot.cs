namespace Karajob.Domain.Hypotheses;

public record DecisionSnapshot(
  DateTime PeriodStartedAt,
  int MarketVacancies,
  int MarketThreshold,
  int PositiveApplications,
  int MaturedApplications,
  int ConversionThresholdPermille,
  int? WellBeingTenths,
  DecisionHint Hint);
