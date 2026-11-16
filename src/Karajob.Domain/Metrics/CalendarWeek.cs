using Karajob.Domain.Common;

namespace Karajob.Domain.Metrics;

public readonly record struct CalendarWeek(DateOnly FirstDay, DateTime Start, DateTime End)
{
  public static CalendarWeek Containing(DateTime utc, TimeZoneInfo zone)
  {
    UtcGuard.Ensure(utc);
    var daysSinceMonday = ((int)utc.DayOfWeek + 6) % 7;
    var start = DateTime.SpecifyKind(utc.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    return new CalendarWeek(DateOnly.FromDateTime(start), start, start.AddDays(7));
  }
}
