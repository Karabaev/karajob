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

  // The autocomplete searches by the field text, which for a selected zone is its full label; without this
  // the list would open with only that zone.
  public static IEnumerable<string> Search(IReadOnlyList<TimeZoneOption> zones, string? text)
  {
    var query = text?.Trim();
    var showAll = string.IsNullOrEmpty(query)
      || zones.Any(zone => string.Equals(zone.Label, query, StringComparison.Ordinal));
    return zones
      .Where(zone => showAll || zone.Label.Contains(query!, StringComparison.OrdinalIgnoreCase))
      .Select(zone => zone.Id);
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
