using Karajob.Domain.Common;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Web.Components.Common;

namespace Karajob.Tests.Web.Components.Common;

public class UserDatesTests
{
  private static DateTime Utc(int month, int day, int hour = 0, int minute = 0)
  {
    return new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
  }

  private static UserDates Create(string zoneId, DateTime now)
  {
    var currentUser = new FixedCurrentUser(Guid.NewGuid(), UserTime.Find(zoneId));
    return new UserDates(currentUser, new FixedTimeProvider(now));
  }

  [Fact]
  public void Today_EastOfUtcAfterUtcMidnight_ReturnsNextLocalDay()
  {
    var dates = Create("Asia/Tokyo", Utc(11, 15, 20));

    var today = dates.Today();

    Assert.Equal(new DateTime(2026, 11, 16), today);
    Assert.Equal(DateTimeKind.Unspecified, today.Kind);
  }

  [Fact]
  public void Today_WestOfUtcBeforeUtcMidnight_ReturnsPreviousLocalDay()
  {
    var dates = Create("America/Los_Angeles", Utc(11, 16, 5));

    Assert.Equal(new DateTime(2026, 11, 15), dates.Today());
  }

  [Fact]
  public void Today_Utc_ReturnsUtcDate()
  {
    var dates = Create("UTC", Utc(11, 16, 23, 59));

    Assert.Equal(new DateTime(2026, 11, 16), dates.Today());
  }

  [Fact]
  public void ToUtc_LocalToday_ReturnsCurrentInstant()
  {
    var now = Utc(11, 15, 20, 17);
    var dates = Create("Asia/Tokyo", now);

    var result = dates.ToUtc(dates.Today());

    Assert.Equal(now, result);
    Assert.Equal(DateTimeKind.Utc, result.Kind);
  }

  [Fact]
  public void ToUtc_PastDate_ReturnsLocalMidnightInUtc()
  {
    var dates = Create("Europe/Moscow", Utc(11, 16, 10));

    var result = dates.ToUtc(new DateTime(2026, 11, 10));

    Assert.Equal(Utc(11, 9, 21), result);
    Assert.Equal(DateTimeKind.Utc, result.Kind);
  }

  [Fact]
  public void ToUtc_FutureDate_ReturnsLocalMidnightInUtc()
  {
    var dates = Create("Asia/Tokyo", Utc(11, 16, 10));

    Assert.Equal(Utc(11, 19, 15), dates.ToUtc(new DateTime(2026, 11, 20)));
  }

  [Fact]
  public void ToUtc_PickedDateWithTimeOfDay_UsesOnlyTheDate()
  {
    var dates = Create("Europe/Moscow", Utc(11, 16, 10));

    Assert.Equal(Utc(11, 9, 21), dates.ToUtc(new DateTime(2026, 11, 10, 13, 45, 0)));
  }

  [Fact]
  public void ToUtc_UtcZonePastDate_ReturnsUtcMidnight()
  {
    var dates = Create("UTC", Utc(11, 16, 10));

    Assert.Equal(Utc(11, 10), dates.ToUtc(new DateTime(2026, 11, 10)));
  }

  [Fact]
  public void Format_LocalMidnight_ShowsDateOnly()
  {
    var dates = Create("Europe/Moscow", Utc(11, 16));

    Assert.Equal("12.11.2026", dates.Format(Utc(11, 11, 21)));
  }

  [Fact]
  public void Format_UtcMidnightButNotLocalMidnight_ShowsDateAndLocalTime()
  {
    var dates = Create("Europe/Moscow", Utc(11, 16));

    Assert.Equal("12.11.2026 03:00", dates.Format(Utc(11, 12)));
  }

  [Fact]
  public void Format_WithTimeOfDay_ShowsLocalDateAndTime()
  {
    var dates = Create("Asia/Tokyo", Utc(11, 16));

    Assert.Equal("13.11.2026 00:05", dates.Format(Utc(11, 12, 15, 5)));
  }

  [Fact]
  public void Format_UtcZone_ShowsUtcValues()
  {
    var dates = Create("UTC", Utc(11, 16));

    Assert.Equal("12.11.2026 09:05", dates.Format(Utc(11, 12, 9, 5)));
  }

  [Fact]
  public void FormatDate_CrossesMidnightInLocalZone_ShowsLocalDate()
  {
    var dates = Create("Asia/Tokyo", Utc(11, 16));

    Assert.Equal("13.11.2026", dates.FormatDate(Utc(11, 12, 20)));
  }

  [Fact]
  public void FormatDate_WestOfUtc_ShowsPreviousLocalDate()
  {
    var dates = Create("America/Los_Angeles", Utc(11, 16));

    Assert.Equal("11.11.2026", dates.FormatDate(Utc(11, 12, 3)));
  }

  [Fact]
  public void FormatDateTime_LocalMidnight_StillShowsTime()
  {
    var dates = Create("Europe/Moscow", Utc(11, 16));

    Assert.Equal("12.11.2026 00:00", dates.FormatDateTime(Utc(11, 11, 21)));
  }

  [Fact]
  public void FormatDateTime_ConvertsToLocalZone()
  {
    var dates = Create("Asia/Tokyo", Utc(11, 16));

    Assert.Equal("13.11.2026 05:30", dates.FormatDateTime(Utc(11, 12, 20, 30)));
  }
}
