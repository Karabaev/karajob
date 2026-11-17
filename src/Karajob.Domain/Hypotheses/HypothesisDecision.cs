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

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  public HypothesisDecision(
    Guid userId,
    Guid hypothesisId,
    DateTime decidedAt,
    HypothesisDecisionKind kind,
    string? reason,
    string? changes,
    DecisionSnapshot snapshot)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    DecidedAt = UtcGuard.Ensure(decidedAt);
    Kind = kind;
    Reason = BlankToNull(reason);
    Changes = BlankToNull(changes);

    if (kind == HypothesisDecisionKind.Expand && Changes is null) throw new ArgumentException("Changes are required to expand.", nameof(changes));
    if (kind == HypothesisDecisionKind.Disable && Reason is null) throw new ArgumentException("Reason is required to disable.", nameof(reason));

    PeriodStartedAt = UtcGuard.Ensure(snapshot.PeriodStartedAt, nameof(snapshot));
    MarketVacancies = snapshot.MarketVacancies;
    MarketThreshold = snapshot.MarketThreshold;
    PositiveApplications = snapshot.PositiveApplications;
    MaturedApplications = snapshot.MaturedApplications;
    ConversionThresholdPermille = snapshot.ConversionThresholdPermille;
    WellBeingTenths = snapshot.WellBeingTenths;
    Hint = snapshot.Hint;
  }

  private HypothesisDecision()
  {
  }
}
