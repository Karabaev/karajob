namespace Karajob.Domain.Applications;

public static class StaleApplicationRule
{
  public static int IdleDays(DateTime lastActivityUtc, DateTime nowUtc, TimeZoneInfo zone)
  {
    throw new NotImplementedException();
  }

  public static bool IsStale(DateTime lastActivityUtc, DateTime nowUtc, int thresholdDays, TimeZoneInfo zone)
  {
    throw new NotImplementedException();
  }
}
