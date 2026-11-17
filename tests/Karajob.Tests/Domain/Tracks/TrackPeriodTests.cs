using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class TrackPeriodTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static readonly Guid RoleId = Guid.NewGuid();
  private static readonly Guid LanguageId = Guid.NewGuid();

  private static Track Create() => new(Guid.NewGuid(), "Gamedev EU", RoleId, LanguageId, Now.AddDays(-30));

  private static Track CreateWithStatus(TrackStatus status)
  {
    var track = Create();
    track.ChangeStatus(TrackStatus.Active, Now);
    track.ChangeStatus(status, Now);
    return track;
  }

  [Fact]
  public void Ctor_Valid_PeriodStartedAtIsNull()
  {
    Assert.Null(Create().PeriodStartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Expanded)]
  public void ChangeStatus_FirstActivation_SetsStartedAtAndPeriodStartedAt(TrackStatus status)
  {
    var track = Create();

    track.ChangeStatus(status, Now);

    Assert.Equal(Now, track.StartedAt);
    Assert.Equal(Now, track.PeriodStartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Paused)]
  [InlineData(TrackStatus.Disabled)]
  public void ChangeStatus_ToNonRunningStatus_LeavesPeriodStartedAtNull(TrackStatus status)
  {
    var track = Create();

    track.ChangeStatus(status, Now);

    Assert.Null(track.PeriodStartedAt);
  }

  [Fact]
  public void ChangeStatus_ReactivateAfterPause_ChangesNeitherStartedAtNorPeriodStartedAt()
  {
    var track = Create();
    track.ChangeStatus(TrackStatus.Active, Now);
    track.ChangeStatus(TrackStatus.Paused, Now.AddDays(1));

    track.ChangeStatus(TrackStatus.Active, Now.AddDays(2));

    Assert.Equal(Now, track.StartedAt);
    Assert.Equal(Now, track.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandFromActive_SetsExpandedAndRestartsPeriod()
  {
    var track = CreateWithStatus(TrackStatus.Active);
    var later = Now.AddDays(10);

    track.ApplyDecision(TrackDecisionKind.Expand, later);

    Assert.Equal(TrackStatus.Expanded, track.Status);
    Assert.Equal(later, track.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandKeepsStartedAt()
  {
    var track = CreateWithStatus(TrackStatus.Active);

    track.ApplyDecision(TrackDecisionKind.Expand, Now.AddDays(10));

    Assert.Equal(Now, track.StartedAt);
  }

  [Fact]
  public void ApplyDecision_ExpandFromExpanded_RestartsPeriodAgain()
  {
    var track = CreateWithStatus(TrackStatus.Active);
    track.ApplyDecision(TrackDecisionKind.Expand, Now.AddDays(10));
    var later = Now.AddDays(25);

    track.ApplyDecision(TrackDecisionKind.Expand, later);

    Assert.Equal(TrackStatus.Expanded, track.Status);
    Assert.Equal(later, track.PeriodStartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Expanded)]
  public void ApplyDecision_Disable_SetsDisabledAndKeepsPeriod(TrackStatus status)
  {
    var track = CreateWithStatus(status);

    track.ApplyDecision(TrackDecisionKind.Disable, Now.AddDays(10));

    Assert.Equal(TrackStatus.Disabled, track.Status);
    Assert.Equal(Now, track.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ContinueFromActive_ChangesNothing()
  {
    var track = CreateWithStatus(TrackStatus.Active);

    track.ApplyDecision(TrackDecisionKind.Continue, Now.AddDays(10));

    Assert.Equal(TrackStatus.Active, track.Status);
    Assert.Equal(Now, track.PeriodStartedAt);
  }

  [Fact]
  public void ApplyDecision_ContinueFromExpanded_StaysExpanded()
  {
    var track = CreateWithStatus(TrackStatus.Active);
    track.ApplyDecision(TrackDecisionKind.Expand, Now.AddDays(5));

    track.ApplyDecision(TrackDecisionKind.Continue, Now.AddDays(10));

    Assert.Equal(TrackStatus.Expanded, track.Status);
    Assert.Equal(Now.AddDays(5), track.PeriodStartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Draft, TrackDecisionKind.Continue)]
  [InlineData(TrackStatus.Draft, TrackDecisionKind.Expand)]
  [InlineData(TrackStatus.Draft, TrackDecisionKind.Disable)]
  [InlineData(TrackStatus.Paused, TrackDecisionKind.Continue)]
  [InlineData(TrackStatus.Paused, TrackDecisionKind.Expand)]
  [InlineData(TrackStatus.Paused, TrackDecisionKind.Disable)]
  [InlineData(TrackStatus.Disabled, TrackDecisionKind.Continue)]
  [InlineData(TrackStatus.Disabled, TrackDecisionKind.Expand)]
  [InlineData(TrackStatus.Disabled, TrackDecisionKind.Disable)]
  public void ApplyDecision_StatusNotActiveOrExpanded_ThrowsInvalidOperation(TrackStatus status, TrackDecisionKind kind)
  {
    var track = Create();
    track.ChangeStatus(status, Now);

    Assert.Throws<InvalidOperationException>(() => track.ApplyDecision(kind, Now.AddDays(1)));

    Assert.Equal(status, track.Status);
  }

  [Fact]
  public void ApplyDecision_NonUtcNow_Throws()
  {
    var track = CreateWithStatus(TrackStatus.Active);

    Assert.ThrowsAny<ArgumentException>(() =>
      track.ApplyDecision(TrackDecisionKind.Expand, new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Local)));
  }
}
