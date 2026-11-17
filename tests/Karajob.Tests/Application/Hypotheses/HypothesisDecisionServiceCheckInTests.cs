using Karajob.Application.Common;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisDecisionServiceCheckInTests : HypothesisDecisionServiceFixture
{
  [Fact]
  public async Task SaveCheckInAsync_Valid_ReturnsDetailsWithWeekScoresAndAverage()
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 2, 3, "tired"));

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
    var hypothesisId = await CreateActiveAsync();

    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));

    Assert.Equal(1, await CountCheckInsAsync(hypothesisId));
  }

  [Theory]
  [InlineData(9)]
  [InlineData(11)]
  [InlineData(13)]
  [InlineData(15)]
  public async Task SaveCheckInAsync_AnyDayOfCurrentWeek_NormalizesToMonday(int day)
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, day)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_SundayOfPreviousWeek_NormalizesToThatWeeksMonday()
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 8)));

    Assert.Equal(PreviousWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_SameWeekTwice_UpdatesTheSameRow()
  {
    var hypothesisId = await CreateActiveAsync();
    var first = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 10), 2, 2, "first"));

    var second = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 12), 5, 4, "second"));

    Assert.Equal(first.Id, second.Id);
    Assert.Equal(5, second.Motivation);
    Assert.Equal(4, second.Confidence);
    Assert.Equal("second", second.Note);
    Assert.Equal(1, await CountCheckInsAsync(hypothesisId));
    var stored = Assert.Single(await Decisions.ListCheckInsAsync(hypothesisId));
    Assert.Equal(5, stored.Motivation);
    Assert.Equal(45, stored.AverageTenths);
  }

  [Fact]
  public async Task SaveCheckInAsync_DifferentWeeks_CreatesTwoRows()
  {
    var hypothesisId = await CreateActiveAsync();

    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek));

    Assert.Equal(2, await CountCheckInsAsync(hypothesisId));
  }

  [Fact]
  public async Task SaveCheckInAsync_SameWeekOnAnotherHypothesis_CreatesSeparateRow()
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
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(TwoWeeksAgo));

    Assert.Equal(TwoWeeksAgo, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_LastDayOfCurrentWeek_IsAccepted()
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 15)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_FirstDayOfNextWeek_FailsOnDayAndSavesNothing()
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 16))));

    Assert.Contains(nameof(CheckInInput.Day), exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(hypothesisId));
  }

  [Fact]
  public async Task SaveCheckInAsync_LaterToday_IsAcceptedAsCurrentWeek()
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 12)));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_ProfileZoneAlreadyInNextWeek_AcceptsThatWeek()
  {
    var hypothesisId = await CreateActiveAsync();
    Clock.UtcNow = Utc(11, 15, 23);
    var auckland = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    var details = await DecisionsFor(SeedData.OwnerId, auckland).SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 16)));

    Assert.Equal(new DateOnly(2026, 11, 16), details.Week);
  }

  [Fact]
  public async Task SaveCheckInAsync_NextWeekInUtcProfile_IsFutureAtSameInstant()
  {
    var hypothesisId = await CreateActiveAsync();
    Clock.UtcNow = Utc(11, 15, 23);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(new DateOnly(2026, 11, 16))));

    Assert.Contains(nameof(CheckInInput.Day), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  public async Task SaveCheckInAsync_MotivationOutOfRange_FailsOnMotivation(int motivation)
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, motivation, 3)));

    Assert.Contains(nameof(CheckInInput.Motivation), exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CheckInInput.Confidence), exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(hypothesisId));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  public async Task SaveCheckInAsync_ConfidenceOutOfRange_FailsOnConfidence(int confidence)
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 3, confidence)));

    Assert.Contains(nameof(CheckInInput.Confidence), exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CheckInInput.Motivation), exception.Errors.Keys);
  }

  [Fact]
  public async Task SaveCheckInAsync_BothScoresInvalid_ReportsBothFields()
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 0, 9)));

    Assert.Contains(nameof(CheckInInput.Motivation), exception.Errors.Keys);
    Assert.Contains(nameof(CheckInInput.Confidence), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(1, 1, 10)]
  [InlineData(5, 5, 50)]
  public async Task SaveCheckInAsync_ScoresAtBounds_AreAccepted(int motivation, int confidence, int expectedTenths)
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, motivation, confidence));

    Assert.Equal(expectedTenths, details.AverageTenths);
  }

  [Fact]
  public async Task SaveCheckInAsync_BlankNote_IsStoredAsNull()
  {
    var hypothesisId = await CreateActiveAsync();

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, note: "   "));

    Assert.Null(details.Note);
  }

  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public async Task SaveCheckInAsync_ActiveOrExpanded_IsAccepted(HypothesisStatus status)
  {
    var hypothesisId = await CreateActiveAsync(status: status);

    var details = await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));

    Assert.Equal(CurrentWeek, details.Week);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public async Task SaveCheckInAsync_StatusNotAllowed_FailsWithEmptyKeyAndSavesNothing(HypothesisStatus status)
  {
    var hypothesisId = await CreateActiveAsync(status: status);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek)));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Equal(0, await CountCheckInsAsync(hypothesisId));
  }

  [Fact]
  public async Task SaveCheckInAsync_UnknownHypothesis_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.SaveCheckInAsync(Guid.NewGuid(), CheckIn(CurrentWeek)));
  }

  [Fact]
  public async Task SaveCheckInAsync_HypothesisOfAnotherProfile_ThrowsKeyNotFound()
  {
    var other = await CreateProfileAsync();
    var foreign = await CreateActiveAsync("Foreign", other);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Decisions.SaveCheckInAsync(foreign, CheckIn(CurrentWeek)));
  }

  [Fact]
  public async Task ListCheckInsAsync_NoCheckIns_ReturnsEmpty()
  {
    var hypothesisId = await CreateActiveAsync();

    Assert.Empty(await Decisions.ListCheckInsAsync(hypothesisId));
  }

  [Fact]
  public async Task ListCheckInsAsync_SeveralWeeks_NewestWeekFirst()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(TwoWeeksAgo));

    var weeks = (await Decisions.ListCheckInsAsync(hypothesisId)).Select(c => c.Week);

    Assert.Equal([CurrentWeek, PreviousWeek, TwoWeeksAgo], weeks);
  }

  [Fact]
  public async Task ListCheckInsAsync_OtherHypothesisCheckIns_AreExcluded()
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
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek));
    var other = await CreateProfileAsync();

    var visible = await ListOrEmptyAsync(DecisionsFor(other), hypothesisId);

    Assert.Empty(visible);
  }

  private static async Task<IReadOnlyList<CheckInDetails>> ListOrEmptyAsync(HypothesisDecisionService service, Guid hypothesisId)
  {
    try
    {
      return await service.ListCheckInsAsync(hypothesisId);
    }
    catch (KeyNotFoundException)
    {
      return [];
    }
  }
}
