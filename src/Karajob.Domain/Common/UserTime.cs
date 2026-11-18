namespace Karajob.Domain.Common;

public static class UserTime
{
  public const string DefaultTimeZoneId = "UTC";

  public static TimeZoneInfo Find(string timeZoneId)
  {
    if (string.IsNullOrWhiteSpace(timeZoneId)) throw new ArgumentException("A time zone id is required.", nameof(timeZoneId));

    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
    }
    catch (TimeZoneNotFoundException exception)
    {
      throw new ArgumentException($"Unknown time zone '{timeZoneId}'.", nameof(timeZoneId), exception);
    }
  }

  public static void EnsureTimeZoneDatabase()
  {
    EnsureTimeZoneDatabase(TimeZoneInfo.GetSystemTimeZones());
  }

  public static void EnsureTimeZoneDatabase(IReadOnlyCollection<TimeZoneInfo> zones)
  {
    if (zones.Any(zone => !string.Equals(zone.Id, DefaultTimeZoneId, StringComparison.OrdinalIgnoreCase))) return;

    throw new InvalidOperationException(
      "The time zone database is missing: only UTC is available. Install tzdata (/usr/share/zoneinfo) on the host or in the container image.");
  }

  public static string NormalizeId(string timeZoneId)
  {
    var zone = Find(timeZoneId);
    if (string.Equals(zone.Id, DefaultTimeZoneId, StringComparison.OrdinalIgnoreCase)) return DefaultTimeZoneId;
    if (zone.HasIanaId) return zone.Id;

    return TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId)
      ? ianaId
      : throw new ArgumentException($"Time zone '{timeZoneId}' has no IANA id.", nameof(timeZoneId));
  }

  public static DateOnly LocalDate(DateTime utc, TimeZoneInfo zone)
  {
    UtcGuard.Ensure(utc);
    return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
  }

  public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo zone)
  {
    var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    // Some zones jump over local midnight on a DST switch; the day then starts at the first valid local minute.
    while (zone.IsInvalidTime(local))
    {
      local = local.AddMinutes(1);
    }

    return TimeZoneInfo.ConvertTimeToUtc(local, zone);
  }
}
