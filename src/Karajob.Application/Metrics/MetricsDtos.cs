using Karajob.Domain.Metrics;

namespace Karajob.Application.Metrics;

public record HypothesisMetrics(
  Guid HypothesisId,
  DateTime PeriodStart,
  DateTime AsOf,
  UtcWeek Week,
  MarketMetric Market,
  ConversionMetric Conversion,
  int WeeklyApplications);

public record MarketMetric(int VacanciesThisWeek, int ThresholdPerWeek)
{
  public MetricVerdict Verdict => throw new NotImplementedException();
}

public record ConversionMetric(
  int Positive,
  int Matured,
  double? Rate,
  ProportionInterval? Interval,
  int ThresholdPermille,
  int MinSampleSize,
  int MaturationDays,
  bool IsInsufficientData)
{
  public MetricVerdict Verdict => throw new NotImplementedException();
}

public record ProfileActivity(UtcWeek Week, int Applications, int WeeklyTarget)
{
  public MetricVerdict Verdict => throw new NotImplementedException();
}
