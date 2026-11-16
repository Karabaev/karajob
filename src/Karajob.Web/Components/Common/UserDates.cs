using Karajob.Application.Users;
using Karajob.Domain.Common;

namespace Karajob.Web.Components.Common;

// Interactive Server renders on the server (UTC in the container), so local dates come from the profile's time zone, not ToLocalTime().
public class UserDates(ICurrentUser currentUser, TimeProvider timeProvider)
{
  private const string DateFormat = "dd.MM.yyyy";
  private const string DateTimeFormat = "dd.MM.yyyy HH:mm";

  public string Format(DateTime utc)
  {
    var local = ToLocal(utc);
    return local.ToString(local.TimeOfDay == TimeSpan.Zero ? DateFormat : DateTimeFormat);
  }

  public string FormatDate(DateTime utc)
  {
    return ToLocal(utc).ToString(DateFormat);
  }

  public string FormatDateTime(DateTime utc)
  {
    return ToLocal(utc).ToString(DateTimeFormat);
  }

  public DateTime Today()
  {
    return UserTime.LocalDate(Now(), currentUser.TimeZone).ToDateTime(TimeOnly.MinValue);
  }

  // Today keeps the current instant so the event is not placed at midnight; any other day starts at its local midnight.
  public DateTime ToUtc(DateTime pickedDate)
  {
    var now = Now();
    var day = DateOnly.FromDateTime(pickedDate);
    return day == UserTime.LocalDate(now, currentUser.TimeZone) ? now : UserTime.StartOfDayUtc(day, currentUser.TimeZone);
  }

  private DateTime ToLocal(DateTime utc)
  {
    return TimeZoneInfo.ConvertTimeFromUtc(UtcGuard.Ensure(utc), currentUser.TimeZone);
  }

  private DateTime Now()
  {
    return timeProvider.GetUtcNow().UtcDateTime;
  }
}
