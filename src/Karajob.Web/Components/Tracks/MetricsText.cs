using System.Globalization;
using Karajob.Application.Metrics;
using Karajob.Domain.Metrics;
using Karajob.Web.Components.Settings;
using Karajob.Web.Resources;
using MudBlazor;

namespace Karajob.Web.Components.Tracks;

public static class MetricsText
{
  // The UI is Russian regardless of the server culture; the invariant culture would print "3.3".
  private static readonly NumberFormatInfo Numbers = new() { NumberDecimalSeparator = "," };

  public static string Verdict(MetricVerdict verdict)
  {
    return verdict switch
    {
      MetricVerdict.InsufficientData => Localized.Get<TrackStrings>("Verdict.InsufficientData"),
      MetricVerdict.BelowThreshold => Localized.Get<TrackStrings>("Verdict.BelowThreshold"),
      MetricVerdict.OnTrack => Localized.Get<TrackStrings>("Verdict.OnTrack"),
      _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
    };
  }

  public static Color VerdictColor(MetricVerdict verdict)
  {
    return verdict switch
    {
      MetricVerdict.InsufficientData => Color.Default,
      MetricVerdict.BelowThreshold => Color.Warning,
      MetricVerdict.OnTrack => Color.Success,
      _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
    };
  }

  public static string Percent(double rate)
  {
    return $"{PercentNumber(rate)} %";
  }

  public static string Interval(ProportionInterval interval)
  {
    return $"{PercentNumber(interval.Lower)}–{PercentNumber(interval.Upper)} %";
  }

  public static string ThresholdPercent(int permille)
  {
    return $"{ConversionPercent.FromPermille(permille).ToString("0.#", Numbers)} %";
  }

  public static string Week(CalendarWeek week)
  {
    return $"{week.FirstDay:dd.MM}–{week.FirstDay.AddDays(6):dd.MM}";
  }

  public static string WeekOf(DateOnly firstDay)
  {
    return $"{firstDay:dd.MM}–{firstDay.AddDays(6):dd.MM}";
  }

  public static string Tenths(int tenths)
  {
    return (tenths / 10.0).ToString("0.0", Numbers);
  }

  public static string Trend(int tenths)
  {
    return tenths switch
    {
      > 0 => $"▲ {Tenths(tenths)}",
      < 0 => $"▼ {Tenths(-tenths)}",
      _ => "= 0"
    };
  }

  private static string PercentNumber(double rate)
  {
    return (rate * 100).ToString("0.0", Numbers);
  }
}
