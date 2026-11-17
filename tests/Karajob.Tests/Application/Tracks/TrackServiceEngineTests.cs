using Karajob.Application.Common;
using Karajob.Application.Tracks;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Tracks;

public class TrackServiceEngineTests : TrackServiceFixture
{
  [Fact]
  public async Task CreateAsync_WithEngines_PersistsThemAndFindReturnsThem()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnrealEngineId]));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal(new[] { SeedData.UnityEngineId, SeedData.UnrealEngineId }.Order(), found.EngineIds.Order());
    Assert.Equal(created.EngineIds.Order(), found.EngineIds.Order());
  }

  [Fact]
  public async Task CreateAsync_NoEngines_IsValidAndStoresNone()
  {
    var created = await Service.CreateAsync(Input(engineIds: []));

    Assert.Empty((await Service.FindAsync(created.Id))!.EngineIds);
  }

  [Fact]
  public async Task CreateAsync_DuplicateEngineIds_StoresEachOnce()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnityEngineId, SeedData.DefoldEngineId]));

    Assert.Equal(new[] { SeedData.UnityEngineId, SeedData.DefoldEngineId }.Order(), created.EngineIds.Order());
  }

  [Fact]
  public async Task CreateAsync_UnknownEngineId_ThrowsValidationWithKeyAndMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, Guid.NewGuid()])));

    Assert.Equal("Engine not found", exception.Errors[nameof(TrackInput.EngineIds)]);
    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public async Task UpdateAsync_ReplacingEngines_RemovesOldOnes()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnrealEngineId]));

    await Service.UpdateAsync(created.Id, Input(engineIds: [SeedData.UnrealEngineId, SeedData.GodotEngineId]));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.UnrealEngineId, SeedData.GodotEngineId }.Order(), found!.EngineIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.TrackEngines.CountAsync(p => p.TrackId == created.Id));
  }

  [Fact]
  public async Task UpdateAsync_EmptyEngines_ClearsThem()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId]));

    await Service.UpdateAsync(created.Id, Input(engineIds: []));

    Assert.Empty((await Service.FindAsync(created.Id))!.EngineIds);
  }

  [Fact]
  public async Task UpdateAsync_UnknownEngineId_ThrowsValidationWithKeyAndKeepsOldEngines()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId]));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(engineIds: [Guid.NewGuid()])));

    Assert.Equal("Engine not found", exception.Errors[nameof(TrackInput.EngineIds)]);
    Assert.Equal([SeedData.UnityEngineId], (await Service.FindAsync(created.Id))!.EngineIds);
  }
}
