using Karajob.Domain.Metrics;

namespace Karajob.Application.Metrics;

public record TrackMetrics(
  Guid TrackId,
  DateTime PeriodStart,
  DateTime AsOf,
  CalendarWeek Week,
  MarketMetric Market,
  ConversionMetric Conversion,
  int WeeklyApplications);

public record MarketMetric(int VacanciesThisWeek, int ThresholdPerWeek)
{
  public MetricVerdict Verdict => VacanciesThisWeek >= ThresholdPerWeek ? MetricVerdict.OnTrack : MetricVerdict.BelowThreshold;
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
  public MetricVerdict Verdict
  {
    get
    {
      if (IsInsufficientData) return MetricVerdict.InsufficientData;

      // Integer comparison of k/n against the permille threshold: no rounding at the boundary.
      return Positive * 1000L >= (long)ThresholdPermille * Matured ? MetricVerdict.OnTrack : MetricVerdict.BelowThreshold;
    }
  }
}

public record ProfileActivity(CalendarWeek Week, int Applications, int WeeklyTarget)
{
  public MetricVerdict Verdict => Applications >= WeeklyTarget ? MetricVerdict.OnTrack : MetricVerdict.BelowThreshold;
}
