using Karajob.Application.Common;
using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Tracks;

public class TrackServiceStatusTests : TrackServiceFixture
{
  private static readonly DateOnly Monday = new(2026, 11, 9);

  [Theory]
  [InlineData(TrackStatus.Expanded)]
  [InlineData(TrackStatus.Disabled)]
  public async Task CreateAsync_ExpandedOrDisabled_FailsOnStatus(TrackStatus status)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(status: status)));

    Assert.Contains(nameof(TrackInput.Status), exception.Errors.Keys);
    Assert.Empty(await Service.ListAsync());
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Paused)]
  public async Task CreateAsync_DraftActiveOrPaused_Succeeds(TrackStatus status)
  {
    var created = await Service.CreateAsync(Input(status: status));

    Assert.Equal(status, created.Status);
  }

  [Theory]
  [InlineData(TrackStatus.Draft, TrackStatus.Expanded)]
  [InlineData(TrackStatus.Draft, TrackStatus.Disabled)]
  [InlineData(TrackStatus.Active, TrackStatus.Expanded)]
  [InlineData(TrackStatus.Active, TrackStatus.Disabled)]
  [InlineData(TrackStatus.Paused, TrackStatus.Expanded)]
  public async Task UpdateAsync_ChangeToExpandedOrDisabled_FailsOnStatusAndKeepsStored(TrackStatus stored, TrackStatus requested)
  {
    var created = await Service.CreateAsync(Input(status: stored));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(status: requested)));

    Assert.Contains(nameof(TrackInput.Status), exception.Errors.Keys);
    Assert.Equal(stored, (await Service.FindAsync(created.Id))!.Status);
  }

  [Fact]
  public async Task UpdateAsync_ExpandedToDisabled_FailsOnStatus()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Expanded);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(id, Input(name: "Gamedev EU", status: TrackStatus.Disabled)));

    Assert.Contains(nameof(TrackInput.Status), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(TrackStatus.Expanded)]
  [InlineData(TrackStatus.Disabled)]
  public async Task UpdateAsync_KeepingStatusSetByDecision_Succeeds(TrackStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var updated = await Service.UpdateAsync(id, Input(name: "Gamedev EU renamed", status: status, positioning: "Senior"));

    Assert.Equal(status, updated.Status);
    Assert.Equal("Gamedev EU renamed", updated.Name);
    Assert.Equal("Senior", (await Service.FindAsync(id))!.Positioning);
  }

  [Theory]
  [InlineData(TrackStatus.Expanded, TrackStatus.Paused)]
  [InlineData(TrackStatus.Expanded, TrackStatus.Active)]
  [InlineData(TrackStatus.Disabled, TrackStatus.Draft)]
  [InlineData(TrackStatus.Disabled, TrackStatus.Active)]
  public async Task UpdateAsync_LeavingExpandedOrDisabledForAllowedStatus_Succeeds(TrackStatus stored, TrackStatus requested)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", stored);

    var updated = await Service.UpdateAsync(id, Input(name: "Gamedev EU", status: requested));

    Assert.Equal(requested, updated.Status);
  }

  [Fact]
  public async Task FindAsync_DraftTrack_PeriodStartedAtIsNull()
  {
    var created = await Service.CreateAsync(Input());

    Assert.Null((await Service.FindAsync(created.Id))!.PeriodStartedAt);
  }

  [Fact]
  public async Task CreateAsync_Active_ReturnsPeriodStartedAtEqualToStartedAt()
  {
    var created = await Service.CreateAsync(Input(status: TrackStatus.Active));

    Assert.Equal(Now, created.StartedAt);
    Assert.Equal(Now, created.PeriodStartedAt);
    Assert.Equal(Now, (await Service.FindAsync(created.Id))!.PeriodStartedAt);
  }

  [Fact]
  public async Task UpdateAsync_DraftToActiveLater_SetsPeriodStartedAtToThatMoment()
  {
    var created = await Service.CreateAsync(Input());
    Clock.UtcNow = Now.AddDays(4);

    var updated = await Service.UpdateAsync(created.Id, Input(status: TrackStatus.Active));

    Assert.Equal(Now.AddDays(4), updated.PeriodStartedAt);
  }

  [Fact]
  public async Task UpdateAsync_ReactivatedAfterPause_KeepsPeriodStartedAt()
  {
    var created = await Service.CreateAsync(Input(status: TrackStatus.Active));
    Clock.UtcNow = Now.AddDays(2);
    await Service.UpdateAsync(created.Id, Input(status: TrackStatus.Paused));
    Clock.UtcNow = Now.AddDays(5);

    var updated = await Service.UpdateAsync(created.Id, Input(status: TrackStatus.Active));

    Assert.Equal(Now, updated.PeriodStartedAt);
  }

  [Fact]
  public async Task DeleteAsync_WithCheckIn_FailsWithEmptyKeyAndKeepsTrack()
  {
    var created = await Service.CreateAsync(Input(status: TrackStatus.Active));
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      context.TrackCheckIns.Add(new TrackCheckIn(SeedData.OwnerId, created.Id, Monday, 3, 3, null, Now));
      await context.SaveChangesAsync();
    }

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_WithDecision_FailsWithEmptyKeyAndKeepsTrack()
  {
    var created = await Service.CreateAsync(Input(status: TrackStatus.Active));
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      var snapshot = new DecisionSnapshot(Now, 0, 10, 0, 0, 30, null, DecisionHint.Expand);
      context.TrackDecisions.Add(
        new TrackDecision(SeedData.OwnerId, created.Id, Now, TrackDecisionKind.Continue, null, null, snapshot));
      await context.SaveChangesAsync();
    }

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }
}
