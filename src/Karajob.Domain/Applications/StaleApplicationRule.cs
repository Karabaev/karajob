using Karajob.Domain.Common;

namespace Karajob.Domain.Applications;

public static class StaleApplicationRule
{
  public static int IdleDays(DateTime lastActivityUtc, DateTime nowUtc, TimeZoneInfo zone)
  {
    return UserTime.LocalDate(nowUtc, zone).DayNumber - UserTime.LocalDate(lastActivityUtc, zone).DayNumber;
  }

  public static bool IsStale(DateTime lastActivityUtc, DateTime nowUtc, int thresholdDays, TimeZoneInfo zone)
  {
    return IdleDays(lastActivityUtc, nowUtc, zone) >= thresholdDays;
  }
}
