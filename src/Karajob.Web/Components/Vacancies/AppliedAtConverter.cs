namespace Karajob.Web.Components.Vacancies;

public static class AppliedAtConverter
{
  // A user ahead of UTC sees their local "today" as tomorrow in UTC terms; only that one day is clamped, anything further is a real future date.
  public static DateTime ToAppliedAt(DateTime pickedDate, DateTime utcNow)
  {
    var date = pickedDate.Date;
    if (date >= utcNow.Date && date <= utcNow.Date.AddDays(1))
    {
      return utcNow;
    }

    return DateTime.SpecifyKind(date, DateTimeKind.Utc);
  }
}
