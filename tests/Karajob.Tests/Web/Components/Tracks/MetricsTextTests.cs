using System.Globalization;
using Karajob.Application.Metrics;
using Karajob.Domain.Metrics;
using Karajob.Web.Components.Tracks;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Tracks;

public class MetricsTextTests
{
  [Theory]
  [InlineData(MetricVerdict.InsufficientData, "insufficient data")]
  [InlineData(MetricVerdict.BelowThreshold, "below threshold")]
  [InlineData(MetricVerdict.OnTrack, "on track")]
  public void Verdict_ReturnsEnglishLabel(MetricVerdict verdict, string expected)
  {
    Assert.Equal(expected, MetricsText.Verdict(verdict));
  }

  [Theory]
  [InlineData(MetricVerdict.InsufficientData, Color.Default)]
  [InlineData(MetricVerdict.BelowThreshold, Color.Warning)]
  [InlineData(MetricVerdict.OnTrack, Color.Success)]
  public void VerdictColor_ReturnsMatchingColor(MetricVerdict verdict, Color expected)
  {
    Assert.Equal(expected, MetricsText.VerdictColor(verdict));
  }

  [Theory]
  [InlineData(1 / 30.0, "3,3 %")]
  [InlineData(0.0, "0,0 %")]
  [InlineData(0.25, "25,0 %")]
  [InlineData(1.0, "100,0 %")]
  public void Percent_FormatsFractionWithCommaAndSpace(double rate, string expected)
  {
    Assert.Equal(expected, MetricsText.Percent(rate));
  }

  [Theory]
  [InlineData(30, "3 %")]
  [InlineData(25, "2,5 %")]
  [InlineData(0, "0 %")]
  public void ThresholdPercent_FormatsPermilleWithoutTrailingZero(int permille, string expected)
  {
    Assert.Equal(expected, MetricsText.ThresholdPercent(permille));
  }

  [Fact]
  public void Percent_UnderInvariantCulture_StillUsesComma()
  {
    var previous = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    try
    {
      Assert.Equal("25,0 %", MetricsText.Percent(0.25));
    }
    finally
    {
      CultureInfo.CurrentCulture = previous;
    }
  }

  [Fact]
  public void Interval_FormatsBoundsWithEnDash()
  {
    Assert.Equal("0,6–16,7 %", MetricsText.Interval(new ProportionInterval(0.006, 0.167)));
  }

  [Fact]
  public void Week_FormatsStartAndLastInclusiveDay()
  {
    var week = CalendarWeek.Containing(new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Utc);

    Assert.Equal("09.11–15.11", MetricsText.Week(week));
  }

  [Fact]
  public void Week_AcrossMonthBoundary_ShowsEachMonth()
  {
    var week = CalendarWeek.Containing(new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Utc);

    Assert.Equal("30.11–06.12", MetricsText.Week(week));
  }
}
