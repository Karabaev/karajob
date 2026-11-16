namespace Karajob.Domain.Common;

public static class UserTime
{
  public const string DefaultTimeZoneId = "UTC";

  public static TimeZoneInfo Find(string timeZoneId)
  {
    throw new NotImplementedException();
  }

  public static string NormalizeId(string timeZoneId)
  {
    return timeZoneId;
  }

  public static DateOnly LocalDate(DateTime utc, TimeZoneInfo zone)
  {
    throw new NotImplementedException();
  }

  public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo zone)
  {
    throw new NotImplementedException();
  }
}
