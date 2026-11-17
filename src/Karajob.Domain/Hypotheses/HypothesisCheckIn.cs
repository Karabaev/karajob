using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class HypothesisCheckIn : IUserOwned
{
  public const int MinScore = 1;
  public const int MaxScore = 5;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public DateOnly Week { get; private set; }
  public int Motivation { get; private set; }
  public int Confidence { get; private set; }
  public string? Note { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime UpdatedAt { get; private set; }

  public void Update(int motivation, int confidence, string? note, DateTime utcNow)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(motivation, MinScore);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(motivation, MaxScore);
    ArgumentOutOfRangeException.ThrowIfLessThan(confidence, MinScore);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(confidence, MaxScore);
    var updatedAt = UtcGuard.Ensure(utcNow);

    Motivation = motivation;
    Confidence = confidence;
    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    UpdatedAt = updatedAt;
  }

  public HypothesisCheckIn(Guid userId, Guid hypothesisId, DateOnly week, int motivation, int confidence, string? note, DateTime createdAt)
  {
    if (week.DayOfWeek != DayOfWeek.Monday) throw new ArgumentException("Week must start on Monday.", nameof(week));

    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    Week = week;
    CreatedAt = UtcGuard.Ensure(createdAt);
    Update(motivation, confidence, note, createdAt);
  }

  private HypothesisCheckIn()
  {
  }
}
