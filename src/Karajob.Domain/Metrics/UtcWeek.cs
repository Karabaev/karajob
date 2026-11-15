using Karajob.Domain.Common;

namespace Karajob.Domain.Metrics;

public readonly record struct UtcWeek(DateTime Start, DateTime End)
{
  public static UtcWeek Containing(DateTime utc)
  {
    UtcGuard.Ensure(utc);
    var daysSinceMonday = ((int)utc.DayOfWeek + 6) % 7;
    var start = DateTime.SpecifyKind(utc.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    return new UtcWeek(start, start.AddDays(7));
  }
}
