using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class TrackCheckInTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
  private static readonly DateOnly Monday = new(2026, 11, 9);

  private static TrackCheckIn Create(int motivation = 3, int confidence = 4, string? note = null)
  {
    return new TrackCheckIn(Guid.NewGuid(), Guid.NewGuid(), Monday, motivation, confidence, note, Now);
  }

  [Fact]
  public void Ctor_Valid_SetsFieldsAndGeneratesId()
  {
    var userId = Guid.NewGuid();
    var trackId = Guid.NewGuid();

    var checkIn = new TrackCheckIn(userId, trackId, Monday, 3, 4, "ok", Now);

    Assert.NotEqual(Guid.Empty, checkIn.Id);
    Assert.Equal(userId, checkIn.UserId);
    Assert.Equal(trackId, checkIn.TrackId);
    Assert.Equal(Monday, checkIn.Week);
    Assert.Equal(3, checkIn.Motivation);
    Assert.Equal(4, checkIn.Confidence);
    Assert.Equal("ok", checkIn.Note);
    Assert.Equal(Now, checkIn.CreatedAt);
    Assert.Equal(Now, checkIn.UpdatedAt);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new TrackCheckIn(Guid.Empty, Guid.NewGuid(), Monday, 3, 3, null, Now));
  }

  [Fact]
  public void Ctor_EmptyTrackId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new TrackCheckIn(Guid.NewGuid(), Guid.Empty, Monday, 3, 3, null, Now));
  }

  [Theory]
  [InlineData(10)]
  [InlineData(11)]
  [InlineData(13)]
  [InlineData(15)]
  public void Ctor_WeekIsNotMonday_ThrowsArgumentException(int day)
  {
    Assert.Throws<ArgumentException>(() =>
      new TrackCheckIn(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, day), 3, 3, null, Now));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(-1)]
  public void Ctor_MotivationOutOfRange_ThrowsArgumentOutOfRange(int motivation)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Create(motivation: motivation));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(-1)]
  public void Ctor_ConfidenceOutOfRange_ThrowsArgumentOutOfRange(int confidence)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Create(confidence: confidence));
  }

  [Theory]
  [InlineData(1, 1)]
  [InlineData(5, 5)]
  public void Ctor_ScoresAtBounds_Accepted(int motivation, int confidence)
  {
    var checkIn = Create(motivation, confidence);

    Assert.Equal(motivation, checkIn.Motivation);
    Assert.Equal(confidence, checkIn.Confidence);
  }

  [Fact]
  public void Ctor_NoteWithWhitespace_IsTrimmed()
  {
    Assert.Equal("felt ok", Create(note: "  felt ok \n").Note);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankNote_BecomesNull(string? note)
  {
    Assert.Null(Create(note: note).Note);
  }

  [Fact]
  public void Ctor_NonUtcCreatedAt_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new TrackCheckIn(Guid.NewGuid(), Guid.NewGuid(), Monday, 3, 3, null, new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Local)));
  }

  [Fact]
  public void Update_Valid_ReplacesValuesAndSetsUpdatedAt()
  {
    var checkIn = Create(3, 4, "old");
    var later = Now.AddDays(2);

    checkIn.Update(1, 2, " new ", later);

    Assert.Equal(1, checkIn.Motivation);
    Assert.Equal(2, checkIn.Confidence);
    Assert.Equal("new", checkIn.Note);
    Assert.Equal(later, checkIn.UpdatedAt);
  }

  [Fact]
  public void Update_Valid_KeepsWeekAndCreatedAt()
  {
    var checkIn = Create();

    checkIn.Update(1, 2, null, Now.AddDays(2));

    Assert.Equal(Monday, checkIn.Week);
    Assert.Equal(Now, checkIn.CreatedAt);
  }

  [Fact]
  public void Update_BlankNote_ClearsNote()
  {
    var checkIn = Create(note: "old");

    checkIn.Update(3, 3, "  ", Now.AddDays(1));

    Assert.Null(checkIn.Note);
  }

  [Theory]
  [InlineData(0, 3)]
  [InlineData(6, 3)]
  [InlineData(3, 0)]
  [InlineData(3, 6)]
  public void Update_ScoreOutOfRange_ThrowsAndKeepsValues(int motivation, int confidence)
  {
    var checkIn = Create(3, 4);

    Assert.Throws<ArgumentOutOfRangeException>(() => checkIn.Update(motivation, confidence, null, Now.AddDays(1)));

    Assert.Equal(3, checkIn.Motivation);
    Assert.Equal(4, checkIn.Confidence);
  }

  [Fact]
  public void Update_NonUtcNow_Throws()
  {
    var checkIn = Create();

    Assert.ThrowsAny<ArgumentException>(() =>
      checkIn.Update(3, 3, null, new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Local)));
  }
}
