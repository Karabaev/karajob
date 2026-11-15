using Karajob.Domain.Metrics;

namespace Karajob.Tests.Domain.Metrics;

public class UtcWeekTests
{
  private static readonly DateTime Monday = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Containing_Wednesday_StartsOnMondayMidnight()
  {
    var week = UtcWeek.Containing(new DateTime(2026, 11, 11, 15, 30, 0, DateTimeKind.Utc));

    Assert.Equal(Monday, week.Start);
  }

  [Fact]
  public void Containing_Wednesday_EndsSevenDaysAfterStart()
  {
    var week = UtcWeek.Containing(new DateTime(2026, 11, 11, 15, 30, 0, DateTimeKind.Utc));

    Assert.Equal(Monday.AddDays(7), week.End);
  }

  [Fact]
  public void Containing_SundayLastSecond_BelongsToPreviousMonday()
  {
    var week = UtcWeek.Containing(new DateTime(2026, 11, 15, 23, 59, 59, DateTimeKind.Utc));

    Assert.Equal(Monday, week.Start);
  }

  [Fact]
  public void Containing_MondayMidnight_StartsAtItself()
  {
    var week = UtcWeek.Containing(Monday);

    Assert.Equal(Monday, week.Start);
  }

  [Fact]
  public void Containing_NextMondayMidnight_StartsNewWeek()
  {
    var week = UtcWeek.Containing(Monday.AddDays(7));

    Assert.Equal(Monday.AddDays(7), week.Start);
  }

  [Fact]
  public void Containing_NewYearThursday_StartsInPreviousYear()
  {
    var week = UtcWeek.Containing(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    Assert.Equal(new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc), week.Start);
  }

  [Fact]
  public void Containing_Utc_ReturnsUtcKinds()
  {
    var week = UtcWeek.Containing(new DateTime(2026, 11, 11, 15, 30, 0, DateTimeKind.Utc));

    Assert.Equal(DateTimeKind.Utc, week.Start.Kind);
    Assert.Equal(DateTimeKind.Utc, week.End.Kind);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Containing_NonUtc_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() => UtcWeek.Containing(new DateTime(2026, 11, 11, 15, 30, 0, kind)));
  }
}
