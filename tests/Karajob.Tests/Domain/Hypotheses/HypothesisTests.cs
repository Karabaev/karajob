using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Domain.Hypotheses;

public class HypothesisTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static readonly Guid RoleId = Guid.NewGuid();
  private static readonly Guid LanguageId = Guid.NewGuid();

  private static Hypothesis Create() => new(Guid.NewGuid(), "Gamedev EU", RoleId, LanguageId, Now);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndDefaults()
  {
    var userId = Guid.NewGuid();

    var hypothesis = new Hypothesis(userId, "Gamedev EU", RoleId, LanguageId, Now);

    Assert.NotEqual(Guid.Empty, hypothesis.Id);
    Assert.Equal(userId, hypothesis.UserId);
    Assert.Equal("Gamedev EU", hypothesis.Name);
    Assert.Equal(RoleId, hypothesis.RoleId);
    Assert.Equal(LanguageId, hypothesis.LanguageId);
    Assert.Equal(Now, hypothesis.CreatedAt);
    Assert.Equal(HypothesisStatus.Draft, hypothesis.Status);
    Assert.Empty(hypothesis.Locations);
    Assert.Empty(hypothesis.Industries);
    Assert.Equal(WorkFormat.None, hypothesis.WorkFormat);
    Assert.Null(hypothesis.StartedAt);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.Empty, "n", RoleId, LanguageId, Now));
  }

  [Fact]
  public void Ctor_BlankName_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.NewGuid(), "  ", RoleId, LanguageId, Now));
  }

  [Fact]
  public void Ctor_EmptyRoleId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.NewGuid(), "n", Guid.Empty, LanguageId, Now));
  }

  [Fact]
  public void Ctor_EmptyLanguageId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.NewGuid(), "n", RoleId, Guid.Empty, Now));
  }

  [Fact]
  public void RoleId_SetEmpty_Throws()
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() => hypothesis.RoleId = Guid.Empty);
  }

  [Fact]
  public void LanguageId_SetEmpty_Throws()
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() => hypothesis.LanguageId = Guid.Empty);
  }

  [Fact]
  public void SetLocations_NewIds_AddsOneEntryPerIdOwnedByHypothesis()
  {
    var hypothesis = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    hypothesis.SetLocations([first, second]);

    Assert.Equal([first, second], hypothesis.Locations.Select(l => l.LocationId).Order());
    Assert.All(hypothesis.Locations, l =>
    {
      Assert.NotEqual(Guid.Empty, l.Id);
      Assert.Equal(hypothesis.UserId, l.UserId);
      Assert.Equal(hypothesis.Id, l.HypothesisId);
    });
  }

  [Fact]
  public void SetLocations_DuplicateIds_KeepsEachOnce()
  {
    var hypothesis = Create();
    var id = Guid.NewGuid();

    hypothesis.SetLocations([id, id, id]);

    Assert.Equal(id, Assert.Single(hypothesis.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsMissingFromNewSet_AreRemoved()
  {
    var hypothesis = Create();
    var kept = Guid.NewGuid();
    var removed = Guid.NewGuid();
    hypothesis.SetLocations([kept, removed]);

    hypothesis.SetLocations([kept]);

    Assert.Equal(kept, Assert.Single(hypothesis.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsThatStay_KeepSameEntryInstance()
  {
    var hypothesis = Create();
    var kept = Guid.NewGuid();
    hypothesis.SetLocations([kept]);
    var original = Assert.Single(hypothesis.Locations);

    hypothesis.SetLocations([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(hypothesis.Locations, l => l.LocationId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(original.Id, stayed.Id);
    Assert.Equal(2, hypothesis.Locations.Count);
  }

  [Fact]
  public void SetLocations_Empty_ClearsLocations()
  {
    var hypothesis = Create();
    hypothesis.SetLocations([Guid.NewGuid(), Guid.NewGuid()]);

    hypothesis.SetLocations([]);

    Assert.Empty(hypothesis.Locations);
  }

  [Fact]
  public void SetLocations_EmptyGuid_Throws()
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() => hypothesis.SetLocations([Guid.NewGuid(), Guid.Empty]));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new Hypothesis(Guid.NewGuid(), "n", RoleId, LanguageId, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void StartedAt_SetNonUtc_Throws(DateTimeKind kind)
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() => hypothesis.StartedAt = new DateTime(2026, 11, 12, 10, 0, 0, kind));
  }

  [Fact]
  public void StartedAt_SetUtc_Succeeds()
  {
    var hypothesis = Create();

    hypothesis.StartedAt = Now;

    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void StartedAt_SetNull_Succeeds()
  {
    var hypothesis = Create();
    hypothesis.StartedAt = Now;

    hypothesis.StartedAt = null;

    Assert.Null(hypothesis.StartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public void ChangeStatus_ToActiveOrExpandedWithoutStart_SetsStartedAt(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Equal(status, hypothesis.Status);
    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public void ChangeStatus_ToNonRunningStatus_LeavesStartedAtNull(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Equal(status, hypothesis.Status);
    Assert.Null(hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ReactivateAfterPause_KeepsOriginalStartedAt()
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);
    hypothesis.ChangeStatus(HypothesisStatus.Paused, Now.AddDays(1));

    hypothesis.ChangeStatus(HypothesisStatus.Active, Now.AddDays(2));

    Assert.Equal(HypothesisStatus.Active, hypothesis.Status);
    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveToExpanded_KeepsStartedAt()
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);

    hypothesis.ChangeStatus(HypothesisStatus.Expanded, Now.AddDays(3));

    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveWithNonUtcNow_Throws()
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() =>
      hypothesis.ChangeStatus(HypothesisStatus.Active, new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Local)));
  }
}
