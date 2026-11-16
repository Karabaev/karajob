using Karajob.Domain.Common;
using Karajob.Domain.Metrics;

namespace Karajob.Tests.Domain.Metrics;

public class CalendarWeekTests
{
  private static readonly DateTime Monday = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

  private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
  {
    return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
  }

  [Fact]
  public void Containing_UtcWednesday_StartsOnMondayMidnight()
  {
    var week = CalendarWeek.Containing(Utc(2026, 11, 11, 15, 30), TimeZoneInfo.Utc);

    Assert.Equal(Monday, week.Start);
    Assert.Equal(new DateOnly(2026, 11, 9), week.FirstDay);
  }

  [Fact]
  public void Containing_UtcWednesday_EndsSevenDaysAfterStart()
  {
    var week = CalendarWeek.Containing(Utc(2026, 11, 11, 15, 30), TimeZoneInfo.Utc);

    Assert.Equal(Monday.AddDays(7), week.End);
  }

  [Fact]
  public void Containing_UtcSundayLastSecond_BelongsToPreviousMonday()
  {
    var week = CalendarWeek.Containing(Utc(2026, 11, 15, 23, 59, 59), TimeZoneInfo.Utc);

    Assert.Equal(Monday, week.Start);
  }

  [Fact]
  public void Containing_UtcMondayMidnight_StartsAtItself()
  {
    var week = CalendarWeek.Containing(Monday, TimeZoneInfo.Utc);

    Assert.Equal(Monday, week.Start);
  }

  [Fact]
  public void Containing_UtcNextMondayMidnight_StartsNewWeek()
  {
    var week = CalendarWeek.Containing(Monday.AddDays(7), TimeZoneInfo.Utc);

    Assert.Equal(Monday.AddDays(7), week.Start);
    Assert.Equal(new DateOnly(2026, 11, 16), week.FirstDay);
  }

  [Fact]
  public void Containing_UtcNewYearThursday_StartsInPreviousYear()
  {
    var week = CalendarWeek.Containing(Utc(2026, 1, 1, 12), TimeZoneInfo.Utc);

    Assert.Equal(Utc(2025, 12, 29), week.Start);
    Assert.Equal(new DateOnly(2025, 12, 29), week.FirstDay);
  }

  [Fact]
  public void Containing_Utc_ReturnsUtcKinds()
  {
    var week = CalendarWeek.Containing(Utc(2026, 11, 11, 15, 30), TimeZoneInfo.Utc);

    Assert.Equal(DateTimeKind.Utc, week.Start.Kind);
    Assert.Equal(DateTimeKind.Utc, week.End.Kind);
  }

  [Fact]
  public void Containing_TokyoLocalMondayJustAfterMidnight_BelongsToThatMonday()
  {
    var zone = UserTime.Find("Asia/Tokyo");

    var week = CalendarWeek.Containing(Utc(2026, 11, 15, 15, 30), zone);

    Assert.Equal(new DateOnly(2026, 11, 16), week.FirstDay);
    Assert.Equal(Utc(2026, 11, 15, 15), week.Start);
    Assert.Equal(Utc(2026, 11, 22, 15), week.End);
  }

  [Fact]
  public void Containing_TokyoLastMinuteBeforeLocalMonday_BelongsToPreviousWeek()
  {
    var zone = UserTime.Find("Asia/Tokyo");

    var week = CalendarWeek.Containing(Utc(2026, 11, 15, 14, 59), zone);

    Assert.Equal(new DateOnly(2026, 11, 9), week.FirstDay);
  }

  [Fact]
  public void Containing_LosAngelesLocalSundayEvening_BelongsToPreviousMonday()
  {
    var zone = UserTime.Find("America/Los_Angeles");

    var week = CalendarWeek.Containing(Utc(2026, 11, 16, 4), zone);

    Assert.Equal(new DateOnly(2026, 11, 9), week.FirstDay);
    Assert.Equal(Utc(2026, 11, 9, 8), week.Start);
    Assert.Equal(Utc(2026, 11, 16, 8), week.End);
  }

  [Fact]
  public void Containing_BerlinWeekWithSpringForward_Lasts167Hours()
  {
    var zone = UserTime.Find("Europe/Berlin");

    var week = CalendarWeek.Containing(Utc(2026, 3, 29, 12), zone);

    Assert.Equal(new DateOnly(2026, 3, 23), week.FirstDay);
    Assert.Equal(TimeSpan.FromHours(167), week.End - week.Start);
  }

  [Fact]
  public void Containing_BerlinWeekWithFallBack_Lasts169Hours()
  {
    var zone = UserTime.Find("Europe/Berlin");

    var week = CalendarWeek.Containing(Utc(2026, 10, 25, 12), zone);

    Assert.Equal(new DateOnly(2026, 10, 19), week.FirstDay);
    Assert.Equal(TimeSpan.FromHours(169), week.End - week.Start);
  }

  [Fact]
  public void Containing_NonUtcZone_ReturnsUtcKinds()
  {
    var week = CalendarWeek.Containing(Utc(2026, 11, 11, 15), UserTime.Find("Europe/Moscow"));

    Assert.Equal(DateTimeKind.Utc, week.Start.Kind);
    Assert.Equal(DateTimeKind.Utc, week.End.Kind);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Containing_NonUtcKind_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() => CalendarWeek.Containing(new DateTime(2026, 11, 11, 15, 30, 0, kind), TimeZoneInfo.Utc));
  }
}
