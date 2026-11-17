using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Domain.Hypotheses;

public class HypothesisDecisionTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
  private static readonly DateTime PeriodStart = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

  private static DecisionSnapshot Snapshot(DateTime? periodStart = null)
  {
    return new DecisionSnapshot(periodStart ?? PeriodStart, 7, 10, 3, 40, 30, 25, DecisionHint.ReviseCv);
  }

  private static HypothesisDecision Create(
    HypothesisDecisionKind kind = HypothesisDecisionKind.Continue,
    string? reason = null,
    string? changes = null)
  {
    return new HypothesisDecision(Guid.NewGuid(), Guid.NewGuid(), Now, kind, reason, changes, Snapshot());
  }

  [Fact]
  public void Ctor_Valid_SetsIdentityAndKind()
  {
    var userId = Guid.NewGuid();
    var hypothesisId = Guid.NewGuid();

    var decision = new HypothesisDecision(userId, hypothesisId, Now, HypothesisDecisionKind.Continue, null, null, Snapshot());

    Assert.NotEqual(Guid.Empty, decision.Id);
    Assert.Equal(userId, decision.UserId);
    Assert.Equal(hypothesisId, decision.HypothesisId);
    Assert.Equal(Now, decision.DecidedAt);
    Assert.Equal(HypothesisDecisionKind.Continue, decision.Kind);
  }

  [Fact]
  public void Ctor_Valid_CopiesEverySnapshotField()
  {
    var decision = Create();

    Assert.Equal(PeriodStart, decision.PeriodStartedAt);
    Assert.Equal(7, decision.MarketVacancies);
    Assert.Equal(10, decision.MarketThreshold);
    Assert.Equal(3, decision.PositiveApplications);
    Assert.Equal(40, decision.MaturedApplications);
    Assert.Equal(30, decision.ConversionThresholdPermille);
    Assert.Equal(25, decision.WellBeingTenths);
    Assert.Equal(DecisionHint.ReviseCv, decision.Hint);
  }

  [Fact]
  public void Ctor_NoWellBeing_KeepsNull()
  {
    var snapshot = Snapshot() with { WellBeingTenths = null };

    var decision = new HypothesisDecision(Guid.NewGuid(), Guid.NewGuid(), Now, HypothesisDecisionKind.Continue, null, null, snapshot);

    Assert.Null(decision.WellBeingTenths);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new HypothesisDecision(Guid.Empty, Guid.NewGuid(), Now, HypothesisDecisionKind.Continue, null, null, Snapshot()));
  }

  [Fact]
  public void Ctor_EmptyHypothesisId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new HypothesisDecision(Guid.NewGuid(), Guid.Empty, Now, HypothesisDecisionKind.Continue, null, null, Snapshot()));
  }

  [Fact]
  public void Ctor_NonUtcDecidedAt_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new HypothesisDecision(
      Guid.NewGuid(),
      Guid.NewGuid(),
      new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Local),
      HypothesisDecisionKind.Continue,
      null,
      null,
      Snapshot()));
  }

  [Fact]
  public void Ctor_NonUtcSnapshotPeriodStart_Throws()
  {
    var snapshot = Snapshot(new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Local));

    Assert.ThrowsAny<ArgumentException>(() =>
      new HypothesisDecision(Guid.NewGuid(), Guid.NewGuid(), Now, HypothesisDecisionKind.Continue, null, null, snapshot));
  }

  [Fact]
  public void Ctor_ContinueWithoutReasonAndChanges_Succeeds()
  {
    var decision = Create(HypothesisDecisionKind.Continue);

    Assert.Null(decision.Reason);
    Assert.Null(decision.Changes);
  }

  [Fact]
  public void Ctor_ReasonAndChanges_AreTrimmed()
  {
    var decision = Create(HypothesisDecisionKind.Expand, "  slow market ", " widen locations\n");

    Assert.Equal("slow market", decision.Reason);
    Assert.Equal("widen locations", decision.Changes);
  }

  [Fact]
  public void Ctor_BlankReasonAndChanges_BecomeNullForContinue()
  {
    var decision = Create(HypothesisDecisionKind.Continue, "  ", "");

    Assert.Null(decision.Reason);
    Assert.Null(decision.Changes);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_ExpandWithoutChanges_ThrowsArgumentException(string? changes)
  {
    Assert.ThrowsAny<ArgumentException>(() => Create(HypothesisDecisionKind.Expand, "reason", changes));
  }

  [Fact]
  public void Ctor_ExpandWithChangesAndNoReason_Succeeds()
  {
    var decision = Create(HypothesisDecisionKind.Expand, null, "added Serbia");

    Assert.Equal("added Serbia", decision.Changes);
    Assert.Null(decision.Reason);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_DisableWithoutReason_ThrowsArgumentException(string? reason)
  {
    Assert.ThrowsAny<ArgumentException>(() => Create(HypothesisDecisionKind.Disable, reason));
  }

  [Fact]
  public void Ctor_DisableWithReasonAndNoChanges_Succeeds()
  {
    var decision = Create(HypothesisDecisionKind.Disable, "Not a personal fit");

    Assert.Equal("Not a personal fit", decision.Reason);
    Assert.Null(decision.Changes);
  }
}
