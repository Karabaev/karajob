using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class HypothesisDecision : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public DateTime DecidedAt { get; private set; }
  public HypothesisDecisionKind Kind { get; private set; }
  public string? Reason { get; private set; }
  public string? Changes { get; private set; }
  public DateTime PeriodStartedAt { get; private set; }
  public int MarketVacancies { get; private set; }
  public int MarketThreshold { get; private set; }
  public int PositiveApplications { get; private set; }
  public int MaturedApplications { get; private set; }
  public int ConversionThresholdPermille { get; private set; }
  public int? WellBeingTenths { get; private set; }
  public DecisionHint Hint { get; private set; }

  public HypothesisDecision(
    Guid userId,
    Guid hypothesisId,
    DateTime decidedAt,
    HypothesisDecisionKind kind,
    string? reason,
    string? changes,
    DecisionSnapshot snapshot)
  {
    throw new NotImplementedException();
  }

  private HypothesisDecision()
  {
  }
}
