using Karajob.Domain.Common;

namespace Karajob.Web.Components.Profiles;

public record TimeZoneOption(string Id, string Label)
{
  // Windows lists its own ids; they are mapped to IANA so the stored value is the same on every host.
  public static IReadOnlyList<TimeZoneOption> All(DateTime utcNow)
  {
    return TimeZoneInfo.GetSystemTimeZones()
      .Select(zone => TryNormalize(zone.Id))
      .OfType<string>()
      .Append(UserTime.DefaultTimeZoneId)
      .Distinct(StringComparer.Ordinal)
      .Select(id => (Id: id, Offset: UserTime.Find(id).GetUtcOffset(utcNow)))
      .OrderBy(zone => zone.Offset)
      .ThenBy(zone => zone.Id, StringComparer.Ordinal)
      .Select(zone => new TimeZoneOption(zone.Id, $"(UTC{FormatOffset(zone.Offset)}) {zone.Id}"))
      .ToList();
  }

  private static string? TryNormalize(string id)
  {
    try
    {
      return UserTime.NormalizeId(id);
    }
    catch (ArgumentException)
    {
      return null;
    }
  }

  private static string FormatOffset(TimeSpan offset)
  {
    var sign = offset < TimeSpan.Zero ? "-" : "+";
    return $@"{sign}{offset.Duration():hh\:mm}";
  }
}
