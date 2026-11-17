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
    throw new NotImplementedException();
  }

  public HypothesisCheckIn(Guid userId, Guid hypothesisId, DateOnly week, int motivation, int confidence, string? note, DateTime createdAt)
  {
    throw new NotImplementedException();
  }

  private HypothesisCheckIn()
  {
  }
}
