using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceEngineTests : VacancyServiceFixture
{
  [Fact]
  public async Task CreateAsync_WithEngines_PersistsThemAndFindReturnsThem()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.GodotEngineId, SeedData.DefoldEngineId]));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal(new[] { SeedData.GodotEngineId, SeedData.DefoldEngineId }.Order(), found.EngineIds.Order());
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
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnityEngineId]));

    Assert.Equal([SeedData.UnityEngineId], created.EngineIds);
  }

  [Fact]
  public async Task CreateAsync_UnknownEngineId_ThrowsValidationWithKeyAndMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, Guid.NewGuid()])));

    Assert.Equal("Engine not found", exception.Errors[nameof(VacancyInput.EngineIds)]);
  }

  [Fact]
  public async Task UpdateAsync_ReplacingEngines_RemovesOldOnes()
  {
    var created = await Service.CreateAsync(Input(engineIds: [SeedData.UnityEngineId, SeedData.UnrealEngineId]));

    await Service.UpdateAsync(created.Id, Input(engineIds: [SeedData.UnrealEngineId, SeedData.CocosEngineId]));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.UnrealEngineId, SeedData.CocosEngineId }.Order(), found!.EngineIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.VacancyEngines.CountAsync(p => p.VacancyId == created.Id));
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

    Assert.Contains(nameof(VacancyInput.EngineIds), exception.Errors.Keys);
    Assert.Equal([SeedData.UnityEngineId], (await Service.FindAsync(created.Id))!.EngineIds);
  }

  [Fact]
  public async Task QuickAddAsync_NewUrl_CreatesVacancyWithoutEngines()
  {
    var result = await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/quick-engines", null));

    var found = await Service.FindAsync(result.VacancyId);
    Assert.NotNull(found);
    Assert.Empty(found.EngineIds);
  }
}
