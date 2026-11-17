using Karajob.Domain.Common;

namespace Karajob.Domain.Metrics;

public readonly record struct CalendarWeek(DateOnly FirstDay, DateTime Start, DateTime End)
{
  public static DateOnly MondayOf(DateOnly day)
  {
    return day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
  }

  public static CalendarWeek Containing(DateTime utc, TimeZoneInfo zone)
  {
    var monday = MondayOf(UserTime.LocalDate(utc, zone));
    return new CalendarWeek(monday, UserTime.StartOfDayUtc(monday, zone), UserTime.StartOfDayUtc(monday.AddDays(7), zone));
  }
}
