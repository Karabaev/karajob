using Karajob.Application.Metrics;
using Karajob.Domain.Metrics;

namespace Karajob.Tests.Application.Metrics;

public class MetricVerdictTests
{
  [Theory]
  [InlineData(10, 10, MetricVerdict.OnTrack)]
  [InlineData(11, 10, MetricVerdict.OnTrack)]
  [InlineData(9, 10, MetricVerdict.BelowThreshold)]
  [InlineData(0, 10, MetricVerdict.BelowThreshold)]
  public void MarketVerdict_ComparesVacanciesWithThreshold(int vacancies, int threshold, MetricVerdict expected)
  {
    var metric = new MarketMetric(vacancies, threshold);

    Assert.Equal(expected, metric.Verdict);
  }

  [Theory]
  [InlineData(3, 100, MetricVerdict.OnTrack)]
  [InlineData(4, 100, MetricVerdict.OnTrack)]
  [InlineData(2, 100, MetricVerdict.BelowThreshold)]
  [InlineData(0, 30, MetricVerdict.BelowThreshold)]
  public void ConversionVerdict_WithEnoughData_ComparesRateWithThresholdPermille(int positive, int matured, MetricVerdict expected)
  {
    var metric = Conversion(positive, matured, insufficient: false);

    Assert.Equal(expected, metric.Verdict);
  }

  [Fact]
  public void ConversionVerdict_InsufficientDataAboveThreshold_IsInsufficientData()
  {
    var metric = Conversion(3, 4, insufficient: true);

    Assert.Equal(MetricVerdict.InsufficientData, metric.Verdict);
  }

  [Fact]
  public void ConversionVerdict_InsufficientDataWithNoApplications_IsInsufficientData()
  {
    var metric = Conversion(0, 0, insufficient: true);

    Assert.Equal(MetricVerdict.InsufficientData, metric.Verdict);
  }

  [Theory]
  [InlineData(10, 10, MetricVerdict.OnTrack)]
  [InlineData(12, 10, MetricVerdict.OnTrack)]
  [InlineData(9, 10, MetricVerdict.BelowThreshold)]
  [InlineData(0, 0, MetricVerdict.OnTrack)]
  public void ActivityVerdict_ComparesApplicationsWithWeeklyTarget(int applications, int target, MetricVerdict expected)
  {
    var activity = new ProfileActivity(CalendarWeek.Containing(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Utc), applications, target);

    Assert.Equal(expected, activity.Verdict);
  }

  private static ConversionMetric Conversion(int positive, int matured, bool insufficient)
  {
    double? rate = matured == 0 ? null : (double)positive / matured;
    return new ConversionMetric(positive, matured, rate, null, 30, 30, 14, insufficient);
  }
}
