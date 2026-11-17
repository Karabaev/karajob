namespace Karajob.Domain.Tracks;

public record WellBeingCheckIn(DateOnly Week, int Motivation, int Confidence);

public record WellBeingSummary(int? LatestTenths, int? TrendTenths, bool IsLow);

public static class WellBeing
{
  public static int AverageTenths(int motivation, int confidence)
  {
    return (motivation + confidence) * 5;
  }

  public static WellBeingSummary Summarize(IEnumerable<WellBeingCheckIn> checkIns, DateOnly currentWeek, int threshold)
  {
    var ordered = checkIns.OrderBy(c => c.Week).ToList();
    if (ordered.Count == 0) return new WellBeingSummary(null, null, false);

    var latest = ordered[^1];
    var latestTenths = AverageTenths(latest.Motivation, latest.Confidence);
    if (ordered.Count == 1) return new WellBeingSummary(latestTenths, null, false);

    var previous = ordered[^2];
    var previousTenths = AverageTenths(previous.Motivation, previous.Confidence);
    var limit = threshold * 10;
    var isLow = previous.Week.AddDays(7) == latest.Week
      && latest.Week >= currentWeek.AddDays(-7)
      && latestTenths <= limit
      && previousTenths <= limit;

    return new WellBeingSummary(latestTenths, latestTenths - previousTenths, isLow);
  }
}
