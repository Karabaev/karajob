using Karajob.Domain.Common;

namespace Karajob.Domain.Metrics;

public readonly record struct CalendarWeek(DateOnly FirstDay, DateTime Start, DateTime End)
{
  public static CalendarWeek Containing(DateTime utc, TimeZoneInfo zone)
  {
    var localDate = UserTime.LocalDate(utc, zone);
    var daysSinceMonday = ((int)localDate.DayOfWeek + 6) % 7;
    var monday = localDate.AddDays(-daysSinceMonday);
    return new CalendarWeek(monday, UserTime.StartOfDayUtc(monday, zone), UserTime.StartOfDayUtc(monday.AddDays(7), zone));
  }
}
