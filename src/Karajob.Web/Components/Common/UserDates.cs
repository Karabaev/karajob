using Karajob.Application.Users;

namespace Karajob.Web.Components.Common;

public class UserDates(ICurrentUser currentUser, TimeProvider timeProvider)
{
  public string Format(DateTime utc)
  {
    return utc.ToString(utc.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm");
  }

  public string FormatDate(DateTime utc)
  {
    return utc.ToString("dd.MM.yyyy");
  }

  public string FormatDateTime(DateTime utc)
  {
    return utc.ToString("dd.MM.yyyy HH:mm");
  }

  public DateTime Today()
  {
    return timeProvider.GetUtcNow().UtcDateTime.Date;
  }

  public DateTime ToUtc(DateTime pickedDate)
  {
    return DateTime.SpecifyKind(pickedDate.Date, DateTimeKind.Utc);
  }
}
