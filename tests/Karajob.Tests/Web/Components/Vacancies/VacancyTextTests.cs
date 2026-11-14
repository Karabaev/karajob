using Karajob.Web.Components.Vacancies;

namespace Karajob.Tests.Web.Components.Vacancies;

public class VacancyTextTests
{
  [Fact]
  public void Date_MidnightUtc_ShowsDateOnly()
  {
    Assert.Equal("12.11.2026", VacancyText.Date(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc)));
  }

  [Fact]
  public void Date_WithTimeOfDay_ShowsDateAndTime()
  {
    Assert.Equal("12.11.2026 09:05", VacancyText.Date(new DateTime(2026, 11, 12, 9, 5, 0, DateTimeKind.Utc)));
  }
}
