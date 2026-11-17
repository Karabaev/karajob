namespace Karajob.Domain.Hypotheses;

public record WellBeingCheckIn(DateOnly Week, int Motivation, int Confidence);

public record WellBeingSummary(int? LatestTenths, int? TrendTenths, bool IsLow);

public static class WellBeing
{
  public static int AverageTenths(int motivation, int confidence)
  {
    throw new NotImplementedException();
  }

  public static WellBeingSummary Summarize(IEnumerable<WellBeingCheckIn> checkIns, DateOnly currentWeek, int threshold)
  {
    throw new NotImplementedException();
  }
}
