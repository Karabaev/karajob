using Karajob.Application.Common;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisServiceStatusTests : HypothesisServiceFixture
{
  private static readonly DateOnly Monday = new(2026, 11, 9);

  [Theory]
  [InlineData(HypothesisStatus.Expanded)]
  [InlineData(HypothesisStatus.Disabled)]
  public async Task CreateAsync_ExpandedOrDisabled_FailsOnStatus(HypothesisStatus status)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(status: status)));

    Assert.Contains(nameof(HypothesisInput.Status), exception.Errors.Keys);
    Assert.Empty(await Service.ListAsync());
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Paused)]
  public async Task CreateAsync_DraftActiveOrPaused_Succeeds(HypothesisStatus status)
  {
    var created = await Service.CreateAsync(Input(status: status));

    Assert.Equal(status, created.Status);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft, HypothesisStatus.Expanded)]
  [InlineData(HypothesisStatus.Draft, HypothesisStatus.Disabled)]
  [InlineData(HypothesisStatus.Active, HypothesisStatus.Expanded)]
  [InlineData(HypothesisStatus.Active, HypothesisStatus.Disabled)]
  [InlineData(HypothesisStatus.Paused, HypothesisStatus.Expanded)]
  public async Task UpdateAsync_ChangeToExpandedOrDisabled_FailsOnStatusAndKeepsStored(HypothesisStatus stored, HypothesisStatus requested)
  {
    var created = await Service.CreateAsync(Input(status: stored));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(status: requested)));

    Assert.Contains(nameof(HypothesisInput.Status), exception.Errors.Keys);
    Assert.Equal(stored, (await Service.FindAsync(created.Id))!.Status);
  }

  [Fact]
  public async Task UpdateAsync_ExpandedToDisabled_FailsOnStatus()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Expanded);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(id, Input(name: "Gamedev EU", status: HypothesisStatus.Disabled)));

    Assert.Contains(nameof(HypothesisInput.Status), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(HypothesisStatus.Expanded)]
  [InlineData(HypothesisStatus.Disabled)]
  public async Task UpdateAsync_KeepingStatusSetByDecision_Succeeds(HypothesisStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var updated = await Service.UpdateAsync(id, Input(name: "Gamedev EU renamed", status: status, positioning: "Senior"));

    Assert.Equal(status, updated.Status);
    Assert.Equal("Gamedev EU renamed", updated.Name);
    Assert.Equal("Senior", (await Service.FindAsync(id))!.Positioning);
  }

  [Theory]
  [InlineData(HypothesisStatus.Expanded, HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Expanded, HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Disabled, HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Disabled, HypothesisStatus.Active)]
  public async Task UpdateAsync_LeavingExpandedOrDisabledForAllowedStatus_Succeeds(HypothesisStatus stored, HypothesisStatus requested)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", stored);

    var updated = await Service.UpdateAsync(id, Input(name: "Gamedev EU", status: requested));

    Assert.Equal(requested, updated.Status);
  }

  [Fact]
  public async Task FindAsync_DraftHypothesis_PeriodStartedAtIsNull()
  {
    var created = await Service.CreateAsync(Input());

    Assert.Null((await Service.FindAsync(created.Id))!.PeriodStartedAt);
  }

  [Fact]
  public async Task CreateAsync_Active_ReturnsPeriodStartedAtEqualToStartedAt()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Active));

    Assert.Equal(Now, created.StartedAt);
    Assert.Equal(Now, created.PeriodStartedAt);
    Assert.Equal(Now, (await Service.FindAsync(created.Id))!.PeriodStartedAt);
  }

  [Fact]
  public async Task UpdateAsync_DraftToActiveLater_SetsPeriodStartedAtToThatMoment()
  {
    var created = await Service.CreateAsync(Input());
    Clock.UtcNow = Now.AddDays(4);

    var updated = await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Active));

    Assert.Equal(Now.AddDays(4), updated.PeriodStartedAt);
  }

  [Fact]
  public async Task UpdateAsync_ReactivatedAfterPause_KeepsPeriodStartedAt()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Active));
    Clock.UtcNow = Now.AddDays(2);
    await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Paused));
    Clock.UtcNow = Now.AddDays(5);

    var updated = await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Active));

    Assert.Equal(Now, updated.PeriodStartedAt);
  }

  [Fact]
  public async Task DeleteAsync_WithCheckIn_FailsWithEmptyKeyAndKeepsHypothesis()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Active));
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      context.HypothesisCheckIns.Add(new HypothesisCheckIn(SeedData.OwnerId, created.Id, Monday, 3, 3, null, Now));
      await context.SaveChangesAsync();
    }

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_WithDecision_FailsWithEmptyKeyAndKeepsHypothesis()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Active));
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      var snapshot = new DecisionSnapshot(Now, 0, 10, 0, 0, 30, null, DecisionHint.Expand);
      context.HypothesisDecisions.Add(
        new HypothesisDecision(SeedData.OwnerId, created.Id, Now, HypothesisDecisionKind.Continue, null, null, snapshot));
      await context.SaveChangesAsync();
    }

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }
}
