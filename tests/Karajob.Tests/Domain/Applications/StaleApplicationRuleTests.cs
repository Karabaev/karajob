using Karajob.Domain.Applications;
using Karajob.Domain.Common;

namespace Karajob.Tests.Domain.Applications;

public class StaleApplicationRuleTests
{
  private static DateTime Utc(int month, int day, int hour = 0, int minute = 0)
  {
    return new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
  }

  [Fact]
  public void IdleDays_SameDay_IsZero()
  {
    Assert.Equal(0, StaleApplicationRule.IdleDays(Utc(11, 12, 1), Utc(11, 12, 23), TimeZoneInfo.Utc));
  }

  [Fact]
  public void IdleDays_FourteenCalendarDaysApart_IsFourteen()
  {
    Assert.Equal(14, StaleApplicationRule.IdleDays(Utc(10, 29, 9), Utc(11, 12, 10), TimeZoneInfo.Utc));
  }

  [Fact]
  public void IdleDays_LessThanTwentyFourHoursAcrossMidnight_CountsOneDay()
  {
    Assert.Equal(1, StaleApplicationRule.IdleDays(Utc(11, 11, 23, 59), Utc(11, 12, 0, 1), TimeZoneInfo.Utc));
  }

  [Fact]
  public void IdleDays_MoreThanTwentyFourHoursSameLocalTimeOfDay_CountsWholeDays()
  {
    Assert.Equal(1, StaleApplicationRule.IdleDays(Utc(11, 11, 12), Utc(11, 12, 11), TimeZoneInfo.Utc));
  }

  [Fact]
  public void IdleDays_NonUtcZone_UsesLocalDates()
  {
    var moscow = UserTime.Find("Europe/Moscow");

    Assert.Equal(1, StaleApplicationRule.IdleDays(Utc(11, 11, 22), Utc(11, 12, 10), TimeZoneInfo.Utc));
    Assert.Equal(0, StaleApplicationRule.IdleDays(Utc(11, 11, 22), Utc(11, 12, 10), moscow));
  }

  [Fact]
  public void IdleDays_ZoneWestOfUtc_UsesLocalDates()
  {
    var losAngeles = UserTime.Find("America/Los_Angeles");

    Assert.Equal(0, StaleApplicationRule.IdleDays(Utc(11, 12, 7), Utc(11, 12, 20), TimeZoneInfo.Utc));
    Assert.Equal(1, StaleApplicationRule.IdleDays(Utc(11, 12, 7), Utc(11, 12, 20), losAngeles));
  }

  [Fact]
  public void IsStale_OneDayBelowThreshold_IsFalse()
  {
    Assert.False(StaleApplicationRule.IsStale(Utc(10, 30, 9), Utc(11, 12, 10), 14, TimeZoneInfo.Utc));
  }

  [Fact]
  public void IsStale_ExactlyAtThreshold_IsTrue()
  {
    Assert.True(StaleApplicationRule.IsStale(Utc(10, 29, 9), Utc(11, 12, 10), 14, TimeZoneInfo.Utc));
  }

  [Fact]
  public void IsStale_BeyondThreshold_IsTrue()
  {
    Assert.True(StaleApplicationRule.IsStale(Utc(10, 1), Utc(11, 12), 14, TimeZoneInfo.Utc));
  }

  [Fact]
  public void IsStale_ZeroThresholdSameDay_IsTrue()
  {
    Assert.True(StaleApplicationRule.IsStale(Utc(11, 12, 1), Utc(11, 12, 2), 0, TimeZoneInfo.Utc));
  }

  [Fact]
  public void IsStale_BoundaryCrossedOnlyInLocalZone_FollowsLocalDay()
  {
    var moscow = UserTime.Find("Europe/Moscow");
    var lastActivity = Utc(10, 29, 22);
    var now = Utc(11, 12, 10);

    Assert.True(StaleApplicationRule.IsStale(lastActivity, now, 14, TimeZoneInfo.Utc));
    Assert.False(StaleApplicationRule.IsStale(lastActivity, now, 14, moscow));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void IdleDays_NonUtcLastActivity_Throws(DateTimeKind kind)
  {
    var lastActivity = new DateTime(2026, 11, 1, 0, 0, 0, kind);

    Assert.ThrowsAny<ArgumentException>(() => StaleApplicationRule.IdleDays(lastActivity, Utc(11, 12), TimeZoneInfo.Utc));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void IdleDays_NonUtcNow_Throws(DateTimeKind kind)
  {
    var now = new DateTime(2026, 11, 12, 0, 0, 0, kind);

    Assert.ThrowsAny<ArgumentException>(() => StaleApplicationRule.IdleDays(Utc(11, 1), now, TimeZoneInfo.Utc));
  }

  [Fact]
  public void IsStale_NonUtcLastActivity_Throws()
  {
    var lastActivity = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Unspecified);

    Assert.ThrowsAny<ArgumentException>(() => StaleApplicationRule.IsStale(lastActivity, Utc(11, 12), 14, TimeZoneInfo.Utc));
  }
}
