using Karajob.Application.Common;
using Karajob.Application.Goals;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Goals;

public class SearchGoalServiceEngineTests : SearchGoalServiceFixture
{
  [Fact]
  public async Task GetAsync_SeededOwner_HasNoEngines()
  {
    Assert.Empty((await Service.GetAsync()).EngineIds);
  }

  [Fact]
  public async Task UpdateAsync_WithEngines_PersistsThem()
  {
    await Service.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnrealEngineId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.UnityEngineId, SeedData.UnrealEngineId }.Order(), details.EngineIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_ReturnedDetails_ContainEngines()
  {
    var returned = await Service.UpdateAsync(Input(engineIds: [SeedData.DefoldEngineId]));

    Assert.Equal([SeedData.DefoldEngineId], returned.EngineIds);
  }

  [Fact]
  public async Task UpdateAsync_DuplicateEngineIds_StoredOnce()
  {
    await Service.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnityEngineId, SeedData.DefoldEngineId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.UnityEngineId, SeedData.DefoldEngineId }.Order(), details.EngineIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.SearchGoalEngines.CountAsync());
  }

  [Fact]
  public async Task UpdateAsync_SecondUpdateWithOtherEngines_ReplacesEngines()
  {
    await Service.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnrealEngineId]));

    await Service.UpdateAsync(Input(engineIds: [SeedData.UnrealEngineId, SeedData.GodotEngineId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.UnrealEngineId, SeedData.GodotEngineId }.Order(), details.EngineIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_EmptyEngines_ClearsEngines()
  {
    await Service.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId]));

    await Service.UpdateAsync(Input(engineIds: []));

    Assert.Empty((await Service.GetAsync()).EngineIds);
  }

  [Fact]
  public async Task UpdateAsync_UnknownEngine_FailsOnEngineIdsWithMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId, Guid.NewGuid()])));

    Assert.Equal("Engine not found", exception.Errors[nameof(SearchGoalInput.EngineIds)]);
    Assert.Empty((await Service.GetAsync()).EngineIds);
  }

  [Fact]
  public async Task UpdateAsync_OtherProfile_DoesNotChangeFirstProfileEngines()
  {
    var second = ServiceFor(await CreateProfileAsync());

    await second.UpdateAsync(Input(engineIds: [SeedData.UnityEngineId]));

    Assert.Empty((await Service.GetAsync()).EngineIds);
    Assert.Equal([SeedData.UnityEngineId], (await second.GetAsync()).EngineIds);
  }
}
