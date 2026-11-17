using Karajob.Application.Common;
using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Tracks;

public class TrackDecisionServiceCheckInTests : TrackDecisionServiceFixture
{
  [Fact]
  public async Task SaveCheckInAsync_Valid_ReturnsDetailsWithWeekScoresAndAverage()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 2, 3, "tired"));

    Assert.NotEqual(Guid.Empty, details.Id);
    Assert.Equal(CurrentWeek, details.Week);
    Assert.Equal(2, details.Motivation);
    Assert.Equal(3, details.Confidence);
    Assert.Equal("tired", details.Note);
    Assert.Equal(25, details.AverageTenths);
  }

  [Fact]
  public async Task SaveCheckInAsync_Valid_PersistsOneRow()
  {
    var trackId = await CreateActiveAsync();

    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));

    Assert.Equal(1, await CountCheckInsAsync(trackId));
  }

  [Theory]
  [InlineData(9)]
  [InlineData(11)]
  [InlineData(13)]
  [InlineData(15)]
  public async Task SaveCheckInAsync_AnyDayOfCurrentWeek_NormalizesToMonday(int day)
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, day)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_SundayOfPreviousWeek_NormalizesToThatWeeksMonday()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 8)));

    Assert.Equal(PreviousWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_SameWeekTwice_UpdatesTheSameRow()
  {
    var trackId = await CreateActiveAsync();
    var first = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 10), 2, 2, "first"));

    var second = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 12), 5, 4, "second"));

    Assert.Equal(first.Id, second.Id);
    Assert.Equal(5, second.Motivation);
    Assert.Equal(4, second.Confidence);
    Assert.Equal("second", second.Note);
    Assert.Equal(1, await CountCheckInsAsync(trackId));
    var stored = Assert.Single(await Decisions.ListCheckInsAsync(trackId));
    Assert.Equal(5, stored.Motivation);
    Assert.Equal(45, stored.AverageTenths);
  }

  [Fact]
  public async Task SaveCheckInAsync_DifferentWeeks_CreatesTwoRows()
  {
    var trackId = await CreateActiveAsync();

    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek));

    Assert.Equal(2, await CountCheckInsAsync(trackId));
  }

  [Fact]
  public async Task SaveCheckInAsync_SameWeekOnAnotherTrack_CreatesSeparateRow()
  {
    var first = await CreateActiveAsync("First");
    var second = await CreateActiveAsync("Second");

    await Decisions.SaveCheckInAsync(first, CheckIn(CurrentWeek));
    await Decisions.SaveCheckInAsync(second, CheckIn(CurrentWeek));

    Assert.Equal(1, await CountCheckInsAsync(first));
    Assert.Equal(1, await CountCheckInsAsync(second));
  }

  [Fact]
  public async Task SaveCheckInAsync_EarlierWeek_IsAccepted()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(TwoWeeksAgo));

    Assert.Equal(TwoWeeksAgo, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_LastDayOfCurrentWeek_IsAccepted()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 15)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_FirstDayOfNextWeek_FailsOnDayAndSavesNothing()
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 16))));

    Assert.Contains(nameof(CheckInInput.Day), exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(trackId));
  }

  [Fact]
  public async Task SaveCheckInAsync_LaterToday_IsAcceptedAsCurrentWeek()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 12)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_ProfileZoneAlreadyInNextWeek_AcceptsThatWeek()
  {
    var trackId = await CreateActiveAsync();
    Clock.UtcNow = Utc(11, 15, 23);
    var auckland = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    var details = await DecisionsFor(SeedData.OwnerId, auckland).SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 16)));

    Assert.Equal(new DateOnly(2026, 11, 16), details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_NextWeekInUtcProfile_IsFutureAtSameInstant()
  {
    var trackId = await CreateActiveAsync();
    Clock.UtcNow = Utc(11, 15, 23);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(new DateOnly(2026, 11, 16))));

    Assert.Contains(nameof(CheckInInput.Day), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  public async Task SaveCheckInAsync_MotivationOutOfRange_FailsOnMotivation(int motivation)
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, motivation, 3)));

    Assert.Contains(nameof(CheckInInput.Motivation), exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CheckInInput.Confidence), exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(trackId));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  public async Task SaveCheckInAsync_ConfidenceOutOfRange_FailsOnConfidence(int confidence)
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 3, confidence)));

    Assert.Contains(nameof(CheckInInput.Confidence), exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CheckInInput.Motivation), exception.Errors.Keys);
  }

  [Fact]
  public async Task SaveCheckInAsync_BothScoresInvalid_ReportsBothFields()
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 0, 9)));

    Assert.Contains(nameof(CheckInInput.Motivation), exception.Errors.Keys);
    Assert.Contains(nameof(CheckInInput.Confidence), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(1, 1, 10)]
  [InlineData(5, 5, 50)]
  public async Task SaveCheckInAsync_ScoresAtBounds_AreAccepted(int motivation, int confidence, int expectedTenths)
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, motivation, confidence));

    Assert.Equal(expectedTenths, details.AverageTenths);
  }

  [Fact]
  public async Task SaveCheckInAsync_BlankNote_IsStoredAsNull()
  {
    var trackId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, note: "   "));

    Assert.Null(details.Note);
  }

  [Theory]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Expanded)]
  public async Task SaveCheckInAsync_ActiveOrExpanded_IsAccepted(TrackStatus status)
  {
    var trackId = await CreateActiveAsync(status: status);

    var details = await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Paused)]
  [InlineData(TrackStatus.Disabled)]
  public async Task SaveCheckInAsync_StatusNotAllowed_FailsWithEmptyKeyAndSavesNothing(TrackStatus status)
  {
    var trackId = await CreateActiveAsync(status: status);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek)));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(trackId));
  }

  [Fact]
  public async Task SaveCheckInAsync_UnknownTrack_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.SaveCheckInAsync(Guid.NewGuid(), CheckIn(CurrentWeek)));
  }

  [Fact]
  public async Task SaveCheckInAsync_TrackOfAnotherProfile_ThrowsKeyNotFound()
  {
    var other = await CreateProfileAsync();
    var foreign = await CreateActiveAsync("Foreign", other);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.SaveCheckInAsync(foreign, CheckIn(CurrentWeek)));
  }

  [Fact]
  public async Task ListCheckInsAsync_NoCheckIns_ReturnsEmpty()
  {
    var trackId = await CreateActiveAsync();

    Assert.Empty(await Decisions.ListCheckInsAsync(trackId));
  }

  [Fact]
  public async Task ListCheckInsAsync_SeveralWeeks_NewestWeekFirst()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(TwoWeeksAgo));

    var weeks = (await Decisions.ListCheckInsAsync(trackId)).Select(c => c.Week);

    Assert.Equal([CurrentWeek, PreviousWeek, TwoWeeksAgo], weeks);
  }

  [Fact]
  public async Task ListCheckInsAsync_OtherTrackCheckIns_AreExcluded()
  {
    var first = await CreateActiveAsync("First");
    var second = await CreateActiveAsync("Second");
    await Decisions.SaveCheckInAsync(first, CheckIn(CurrentWeek, 1, 1));
    await Decisions.SaveCheckInAsync(second, CheckIn(CurrentWeek, 5, 5));

    var list = await Decisions.ListCheckInsAsync(first);

    var item = Assert.Single(list);
    Assert.Equal(10, item.AverageTenths);
  }

  [Fact]
  public async Task ListCheckInsAsync_AnotherProfile_SeesNothing()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek));
    var other = await CreateProfileAsync();

    var visible = await ListOrEmptyAsync(DecisionsFor(other), trackId);

    Assert.Empty(visible);
  }

  private static async Task<IReadOnlyList<CheckInDetails>> ListOrEmptyAsync(TrackDecisionService service, Guid trackId)
  {
    try
    {
      return await service.ListCheckInsAsync(trackId);
    }
    catch (KeyNotFoundException)
    {
      return [];
    }
  }
}
