using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Domain.Hypotheses;

public class HypothesisPeriodTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static readonly Guid RoleId = Guid.NewGuid();
  private static readonly Guid LanguageId = Guid.NewGuid();

  private static Hypothesis Create() => new(Guid.NewGuid(), "Gamedev EU", RoleId, LanguageId, Now.AddDays(-30));

  private static Hypothesis CreateWithStatus(HypothesisStatus status)
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);
    hypothesis.ChangeStatus(status, Now);
    return hypothesis;
  }

  [Fact]
  public void Ctor_Valid_PeriodStartedAtIsNull()
  {
    Assert.Null(Create().PeriodStartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public void ChangeStatus_FirstActivation_SetsStartedAtAndPeriodStartedAt(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Equal(Now, hypothesis.StartedAt);
    Assert.Equal(Now, hypothesis.PeriodStartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public void ChangeStatus_ToNonRunningStatus_LeavesPeriodStartedAtNull(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Null(hypothesis.PeriodStartedAt);
  }

  [Fact]
  public void ChangeStatus_ReactivateAfterPause_ChangesNeitherStartedAtNorPeriodStartedAt()
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);
    hypothesis.ChangeStatus(HypothesisStatus.Paused, Now.AddDays(1));

    hypothesis.ChangeStatus(HypothesisStatus.Active, Now.AddDays(2));

    Assert.Equal(Now, hypothesis.StartedAt);
    Assert.Equal(Now, hypothesis.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandFromActive_SetsExpandedAndRestartsPeriod()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);
    var later = Now.AddDays(10);

    hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, later);

    Assert.Equal(HypothesisStatus.Expanded, hypothesis.Status);
    Assert.Equal(later, hypothesis.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandKeepsStartedAt()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);

    hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, Now.AddDays(10));

    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandFromExpanded_RestartsPeriodAgain()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);
    hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, Now.AddDays(10));
    var later = Now.AddDays(25);

    hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, later);

    Assert.Equal(HypothesisStatus.Expanded, hypothesis.Status);
    Assert.Equal(later, hypothesis.PeriodStartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public void ApplyDecision_Disable_SetsDisabledAndKeepsPeriod(HypothesisStatus status)
  {
    var hypothesis = CreateWithStatus(status);

    hypothesis.ApplyDecision(HypothesisDecisionKind.Disable, Now.AddDays(10));

    Assert.Equal(HypothesisStatus.Disabled, hypothesis.Status);
    Assert.Equal(Now, hypothesis.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ContinueFromActive_ChangesNothing()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);

    hypothesis.ApplyDecision(HypothesisDecisionKind.Continue, Now.AddDays(10));

    Assert.Equal(HypothesisStatus.Active, hypothesis.Status);
    Assert.Equal(Now, hypothesis.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ContinueFromExpanded_StaysExpanded()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);
    hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, Now.AddDays(5));

    hypothesis.ApplyDecision(HypothesisDecisionKind.Continue, Now.AddDays(10));

    Assert.Equal(HypothesisStatus.Expanded, hypothesis.Status);
    Assert.Equal(Now.AddDays(5), hypothesis.PeriodStartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft, HypothesisDecisionKind.Continue)]
  [InlineData(HypothesisStatus.Draft, HypothesisDecisionKind.Expand)]
  [InlineData(HypothesisStatus.Draft, HypothesisDecisionKind.Disable)]
  [InlineData(HypothesisStatus.Paused, HypothesisDecisionKind.Continue)]
  [InlineData(HypothesisStatus.Paused, HypothesisDecisionKind.Expand)]
  [InlineData(HypothesisStatus.Paused, HypothesisDecisionKind.Disable)]
  [InlineData(HypothesisStatus.Disabled, HypothesisDecisionKind.Continue)]
  [InlineData(HypothesisStatus.Disabled, HypothesisDecisionKind.Expand)]
  [InlineData(HypothesisStatus.Disabled, HypothesisDecisionKind.Disable)]
  public void ApplyDecision_StatusNotActiveOrExpanded_ThrowsInvalidOperation(HypothesisStatus status, HypothesisDecisionKind kind)
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(status, Now);

    Assert.Throws<InvalidOperationException>(() => hypothesis.ApplyDecision(kind, Now.AddDays(1)));

    Assert.Equal(status, hypothesis.Status);
  }

  [Fact]
  public void ApplyDecision_NonUtcNow_Throws()
  {
    var hypothesis = CreateWithStatus(HypothesisStatus.Active);

    Assert.ThrowsAny<ArgumentException>(() =>
      hypothesis.ApplyDecision(HypothesisDecisionKind.Expand, new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Local)));
  }
}
