using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class TrackTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static readonly Guid RoleId = Guid.NewGuid();
  private static readonly Guid LanguageId = Guid.NewGuid();

  private static Track Create() => new(Guid.NewGuid(), "Gamedev EU", RoleId, LanguageId, Now);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndDefaults()
  {
    var userId = Guid.NewGuid();

    var track = new Track(userId, "Gamedev EU", RoleId, LanguageId, Now);

    Assert.NotEqual(Guid.Empty, track.Id);
    Assert.Equal(userId, track.UserId);
    Assert.Equal("Gamedev EU", track.Name);
    Assert.Equal(RoleId, track.RoleId);
    Assert.Equal(LanguageId, track.LanguageId);
    Assert.Equal(Now, track.CreatedAt);
    Assert.Equal(TrackStatus.Draft, track.Status);
    Assert.Empty(track.Locations);
    Assert.Empty(track.Industries);
    Assert.Equal(WorkFormat.None, track.WorkFormat);
    Assert.Null(track.StartedAt);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Track(Guid.Empty, "n", RoleId, LanguageId, Now));
  }

  [Fact]
  public void Ctor_BlankName_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Track(Guid.NewGuid(), "  ", RoleId, LanguageId, Now));
  }

  [Fact]
  public void Ctor_EmptyRoleId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Track(Guid.NewGuid(), "n", Guid.Empty, LanguageId, Now));
  }

  [Fact]
  public void Ctor_EmptyLanguageId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Track(Guid.NewGuid(), "n", RoleId, Guid.Empty, Now));
  }

  [Fact]
  public void RoleId_SetEmpty_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.RoleId = Guid.Empty);
  }

  [Fact]
  public void LanguageId_SetEmpty_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.LanguageId = Guid.Empty);
  }

  [Fact]
  public void SetLocations_NewIds_AddsOneEntryPerIdOwnedByTrack()
  {
    var track = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    track.SetLocations([first, second]);

    Assert.Equal([first, second], track.Locations.Select(l => l.LocationId));
    Assert.All(track.Locations, l =>
    {
      Assert.NotEqual(Guid.Empty, l.Id);
      Assert.Equal(track.UserId, l.UserId);
      Assert.Equal(track.Id, l.TrackId);
    });
  }

  [Fact]
  public void SetLocations_DuplicateIds_KeepsEachOnce()
  {
    var track = Create();
    var id = Guid.NewGuid();

    track.SetLocations([id, id, id]);

    Assert.Equal(id, Assert.Single(track.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsMissingFromNewSet_AreRemoved()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    var removed = Guid.NewGuid();
    track.SetLocations([kept, removed]);

    track.SetLocations([kept]);

    Assert.Equal(kept, Assert.Single(track.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsThatStay_KeepSameEntryInstance()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    track.SetLocations([kept]);
    var original = Assert.Single(track.Locations);

    track.SetLocations([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(track.Locations, l => l.LocationId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(original.Id, stayed.Id);
    Assert.Equal(2, track.Locations.Count);
  }

  [Fact]
  public void SetLocations_Empty_ClearsLocations()
  {
    var track = Create();
    track.SetLocations([Guid.NewGuid(), Guid.NewGuid()]);

    track.SetLocations([]);

    Assert.Empty(track.Locations);
  }

  [Fact]
  public void SetLocations_EmptyGuid_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.SetLocations([Guid.NewGuid(), Guid.Empty]));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new Track(Guid.NewGuid(), "n", RoleId, LanguageId, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void StartedAt_SetNonUtc_Throws(DateTimeKind kind)
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.StartedAt = new DateTime(2026, 11, 12, 10, 0, 0, kind));
  }

  [Fact]
  public void StartedAt_SetUtc_Succeeds()
  {
    var track = Create();

    track.StartedAt = Now;

    Assert.Equal(Now, track.StartedAt);
  }

  [Fact]
  public void StartedAt_SetNull_Succeeds()
  {
    var track = Create();
    track.StartedAt = Now;

    track.StartedAt = null;

    Assert.Null(track.StartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Expanded)]
  public void ChangeStatus_ToActiveOrExpandedWithoutStart_SetsStartedAt(TrackStatus status)
  {
    var track = Create();

    track.ChangeStatus(status, Now);

    Assert.Equal(status, track.Status);
    Assert.Equal(Now, track.StartedAt);
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Paused)]
  [InlineData(TrackStatus.Disabled)]
  public void ChangeStatus_ToNonRunningStatus_LeavesStartedAtNull(TrackStatus status)
  {
    var track = Create();

    track.ChangeStatus(status, Now);

    Assert.Equal(status, track.Status);
    Assert.Null(track.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ReactivateAfterPause_KeepsOriginalStartedAt()
  {
    var track = Create();
    track.ChangeStatus(TrackStatus.Active, Now);
    track.ChangeStatus(TrackStatus.Paused, Now.AddDays(1));

    track.ChangeStatus(TrackStatus.Active, Now.AddDays(2));

    Assert.Equal(TrackStatus.Active, track.Status);
    Assert.Equal(Now, track.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveToExpanded_KeepsStartedAt()
  {
    var track = Create();
    track.ChangeStatus(TrackStatus.Active, Now);

    track.ChangeStatus(TrackStatus.Expanded, Now.AddDays(3));

    Assert.Equal(Now, track.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveWithNonUtcNow_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() =>
      track.ChangeStatus(TrackStatus.Active, new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Local)));
  }

  [Fact]
  public void SetPlatforms_NewIds_AddsOneEntryPerIdOwnedByTrack()
  {
    var track = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    track.SetPlatforms([first, second]);

    Assert.Equal([first, second], track.Platforms.Select(p => p.PlatformId));
    Assert.All(track.Platforms, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(track.UserId, p.UserId);
      Assert.Equal(track.Id, p.TrackId);
    });
  }

  [Fact]
  public void SetPlatforms_DuplicateIds_KeepsEachOnce()
  {
    var track = Create();
    var id = Guid.NewGuid();

    track.SetPlatforms([id, id, id]);

    Assert.Equal(id, Assert.Single(track.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsMissingFromNewSet_AreRemoved()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    track.SetPlatforms([kept, Guid.NewGuid()]);

    track.SetPlatforms([kept]);

    Assert.Equal(kept, Assert.Single(track.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsThatStay_KeepSameEntryInstance()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    track.SetPlatforms([kept]);
    var original = Assert.Single(track.Platforms);

    track.SetPlatforms([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(track.Platforms, p => p.PlatformId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, track.Platforms.Count);
  }

  [Fact]
  public void SetPlatforms_Empty_ClearsPlatforms()
  {
    var track = Create();
    track.SetPlatforms([Guid.NewGuid(), Guid.NewGuid()]);

    track.SetPlatforms([]);

    Assert.Empty(track.Platforms);
  }

  [Fact]
  public void SetPlatforms_EmptyGuid_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.SetPlatforms([Guid.NewGuid(), Guid.Empty]));
  }

  [Fact]
  public void SetEngines_NewIds_AddsOneEntryPerIdOwnedByTrack()
  {
    var track = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    track.SetEngines([first, second]);

    Assert.Equal([first, second], track.Engines.Select(p => p.EngineId));
    Assert.All(track.Engines, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(track.UserId, p.UserId);
      Assert.Equal(track.Id, p.TrackId);
    });
  }

  [Fact]
  public void SetEngines_DuplicateIds_KeepsEachOnce()
  {
    var track = Create();
    var id = Guid.NewGuid();

    track.SetEngines([id, id, id]);

    Assert.Equal(id, Assert.Single(track.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsMissingFromNewSet_AreRemoved()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    track.SetEngines([kept, Guid.NewGuid()]);

    track.SetEngines([kept]);

    Assert.Equal(kept, Assert.Single(track.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsThatStay_KeepSameEntryInstance()
  {
    var track = Create();
    var kept = Guid.NewGuid();
    track.SetEngines([kept]);
    var original = Assert.Single(track.Engines);

    track.SetEngines([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(track.Engines, p => p.EngineId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, track.Engines.Count);
  }

  [Fact]
  public void SetEngines_Empty_ClearsEngines()
  {
    var track = Create();
    track.SetEngines([Guid.NewGuid(), Guid.NewGuid()]);

    track.SetEngines([]);

    Assert.Empty(track.Engines);
  }

  [Fact]
  public void SetEngines_EmptyGuid_Throws()
  {
    var track = Create();

    Assert.ThrowsAny<ArgumentException>(() => track.SetEngines([Guid.NewGuid(), Guid.Empty]));
  }
}
