using System.Globalization;
using Karajob.Application.Metrics;
using Karajob.Domain.Metrics;
using Karajob.Web.Components.Settings;
using MudBlazor;

namespace Karajob.Web.Components.Hypotheses;

public static class MetricsText
{
  // The UI is Russian regardless of the server culture; the invariant culture would print "3.3".
  private static readonly NumberFormatInfo Numbers = new() { NumberDecimalSeparator = "," };

  public static string Verdict(MetricVerdict verdict)
  {
    return verdict switch
    {
      MetricVerdict.InsufficientData => "данных мало",
      MetricVerdict.BelowThreshold => "ниже порога",
      MetricVerdict.OnTrack => "в норме",
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

  public static string Week(UtcWeek week)
  {
    return $"{week.Start:dd.MM}–{week.End.AddDays(-1):dd.MM}";
  }

  private static string PercentNumber(double rate)
  {
    return (rate * 100).ToString("0.0", Numbers);
  }
}
