using Karajob.Web.Components.Vacancies;

namespace Karajob.Tests.Web.Components.Vacancies;

public class AppliedAtConverterTests
{
  private static readonly DateTime UtcNow = new(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);

  [Fact]
  public void ToAppliedAt_Today_ReturnsNow()
  {
    Assert.Equal(UtcNow, AppliedAtConverter.ToAppliedAt(UtcNow.Date, UtcNow));
  }

  [Fact]
  public void ToAppliedAt_PastDay_ReturnsMidnightUtc()
  {
    var result = AppliedAtConverter.ToAppliedAt(new DateTime(2026, 11, 10), UtcNow);

    Assert.Equal(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), result);
    Assert.Equal(DateTimeKind.Utc, result.Kind);
  }

  [Fact]
  public void ToAppliedAt_DayAfterUtcToday_ReturnsNow()
  {
    Assert.Equal(UtcNow, AppliedAtConverter.ToAppliedAt(UtcNow.Date.AddDays(1), UtcNow));
  }

  [Fact]
  public void ToAppliedAt_FurtherInFuture_ReturnsMidnightUtcSoServiceRejectsIt()
  {
    var result = AppliedAtConverter.ToAppliedAt(UtcNow.Date.AddDays(5), UtcNow);

    Assert.Equal(DateTime.SpecifyKind(UtcNow.Date.AddDays(5), DateTimeKind.Utc), result);
  }
}
