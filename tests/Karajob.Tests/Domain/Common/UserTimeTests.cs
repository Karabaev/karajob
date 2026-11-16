using Karajob.Domain.Common;

namespace Karajob.Tests.Domain.Common;

public class UserTimeTests
{
  private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0)
  {
    return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
  }

  [Fact]
  public void DefaultTimeZoneId_IsUtc()
  {
    Assert.Equal("UTC", UserTime.DefaultTimeZoneId);
  }

  [Fact]
  public void LocalDate_EastOfUtcAfterUtcMidnight_ReturnsNextDay()
  {
    var zone = UserTime.Find("Asia/Tokyo");

    Assert.Equal(new DateOnly(2026, 11, 16), UserTime.LocalDate(Utc(2026, 11, 15, 20), zone));
  }

  [Fact]
  public void LocalDate_EastOfUtcBeforeLocalMidnight_ReturnsSameDay()
  {
    var zone = UserTime.Find("Asia/Tokyo");

    Assert.Equal(new DateOnly(2026, 11, 15), UserTime.LocalDate(Utc(2026, 11, 15, 14, 59), zone));
  }

  [Fact]
  public void LocalDate_WestOfUtcBeforeUtcMidnight_ReturnsPreviousDay()
  {
    var zone = UserTime.Find("America/Los_Angeles");

    Assert.Equal(new DateOnly(2026, 11, 15), UserTime.LocalDate(Utc(2026, 11, 16, 5), zone));
  }

  [Fact]
  public void LocalDate_Utc_ReturnsUtcDate()
  {
    Assert.Equal(new DateOnly(2026, 11, 16), UserTime.LocalDate(Utc(2026, 11, 16, 23, 59), TimeZoneInfo.Utc));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void LocalDate_NonUtcKind_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() => UserTime.LocalDate(new DateTime(2026, 11, 16, 5, 0, 0, kind), TimeZoneInfo.Utc));
  }

  [Fact]
  public void StartOfDayUtc_Tokyo_IsNineHoursEarlierThanUtcMidnight()
  {
    var zone = UserTime.Find("Asia/Tokyo");

    Assert.Equal(Utc(2026, 11, 15, 15), UserTime.StartOfDayUtc(new DateOnly(2026, 11, 16), zone));
  }

  [Fact]
  public void StartOfDayUtc_LosAngelesStandardTime_IsEightHoursLater()
  {
    var zone = UserTime.Find("America/Los_Angeles");

    Assert.Equal(Utc(2026, 11, 15, 8), UserTime.StartOfDayUtc(new DateOnly(2026, 11, 15), zone));
  }

  [Fact]
  public void StartOfDayUtc_Utc_ReturnsUtcMidnight()
  {
    Assert.Equal(Utc(2026, 11, 16), UserTime.StartOfDayUtc(new DateOnly(2026, 11, 16), TimeZoneInfo.Utc));
  }

  [Fact]
  public void StartOfDayUtc_AnyZone_ReturnsUtcKind()
  {
    var result = UserTime.StartOfDayUtc(new DateOnly(2026, 11, 16), UserTime.Find("Europe/Moscow"));

    Assert.Equal(DateTimeKind.Utc, result.Kind);
  }

  [Fact]
  public void StartOfDayUtc_MidnightInDstGap_ReturnsFirstValidLocalInstantOfDay()
  {
    var zone = UserTime.Find("America/Santiago");
    Assert.True(zone.IsInvalidTime(new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Unspecified)));

    var result = UserTime.StartOfDayUtc(new DateOnly(2026, 9, 6), zone);

    Assert.Equal(Utc(2026, 9, 6, 4), result);
  }

  [Fact]
  public void Find_Iana_ReturnsZone()
  {
    var zone = UserTime.Find("Europe/Moscow");

    Assert.Equal(TimeSpan.FromHours(3), zone.GetUtcOffset(Utc(2026, 11, 16)));
  }

  [Fact]
  public void Find_Utc_ReturnsZoneWithZeroOffset()
  {
    Assert.Equal(TimeSpan.Zero, UserTime.Find("UTC").GetUtcOffset(Utc(2026, 11, 16)));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("Mars/Olympus_Mons")]
  public void Find_UnknownOrBlank_Throws(string id)
  {
    Assert.ThrowsAny<ArgumentException>(() => UserTime.Find(id));
  }

  [Fact]
  public void NormalizeId_Iana_StaysTheSame()
  {
    Assert.Equal("Europe/Moscow", UserTime.NormalizeId("Europe/Moscow"));
  }

  [Fact]
  public void NormalizeId_Utc_StaysUtc()
  {
    Assert.Equal("UTC", UserTime.NormalizeId("UTC"));
  }

  [Fact]
  public void NormalizeId_WindowsId_ConvertsToIana()
  {
    Assert.Equal("Europe/Moscow", UserTime.NormalizeId("Russian Standard Time"));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("Mars/Olympus_Mons")]
  public void NormalizeId_UnknownOrBlank_Throws(string id)
  {
    Assert.ThrowsAny<ArgumentException>(() => UserTime.NormalizeId(id));
  }
}
