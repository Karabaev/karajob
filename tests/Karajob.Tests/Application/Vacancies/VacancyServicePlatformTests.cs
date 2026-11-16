using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServicePlatformTests : VacancyServiceFixture
{
  [Fact]
  public async Task CreateAsync_WithPlatforms_PersistsThemAndFindReturnsThem()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.MobilePlatformId, SeedData.WebPlatformId]));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal(new[] { SeedData.MobilePlatformId, SeedData.WebPlatformId }.Order(), found.PlatformIds.Order());
    Assert.Equal(created.PlatformIds.Order(), found.PlatformIds.Order());
  }

  [Fact]
  public async Task CreateAsync_NoPlatforms_IsValidAndStoresNone()
  {
    var created = await Service.CreateAsync(Input(platformIds: []));

    Assert.Empty((await Service.FindAsync(created.Id))!.PlatformIds);
  }

  [Fact]
  public async Task CreateAsync_DuplicatePlatformIds_StoresEachOnce()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.PcPlatformId]));

    Assert.Equal([SeedData.PcPlatformId], created.PlatformIds);
  }

  [Fact]
  public async Task CreateAsync_UnknownPlatformId_ThrowsValidationWithKeyAndMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, Guid.NewGuid()])));

    Assert.Equal("Platform not found", exception.Errors[nameof(VacancyInput.PlatformIds)]);
  }

  [Fact]
  public async Task UpdateAsync_ReplacingPlatforms_RemovesOldOnes()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.VrPlatformId]));

    await Service.UpdateAsync(created.Id, Input(platformIds: [SeedData.VrPlatformId, SeedData.ConsolesPlatformId]));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.VrPlatformId, SeedData.ConsolesPlatformId }.Order(), found!.PlatformIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.VacancyPlatforms.CountAsync(p => p.VacancyId == created.Id));
  }

  [Fact]
  public async Task UpdateAsync_EmptyPlatforms_ClearsThem()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId]));

    await Service.UpdateAsync(created.Id, Input(platformIds: []));

    Assert.Empty((await Service.FindAsync(created.Id))!.PlatformIds);
  }

  [Fact]
  public async Task UpdateAsync_UnknownPlatformId_ThrowsValidationWithKeyAndKeepsOldPlatforms()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId]));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(platformIds: [Guid.NewGuid()])));

    Assert.Contains(nameof(VacancyInput.PlatformIds), exception.Errors.Keys);
    Assert.Equal([SeedData.PcPlatformId], (await Service.FindAsync(created.Id))!.PlatformIds);
  }

  [Fact]
  public async Task QuickAddAsync_NewUrl_CreatesVacancyWithoutPlatforms()
  {
    var result = await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/quick-platforms", null));

    var found = await Service.FindAsync(result.VacancyId);
    Assert.NotNull(found);
    Assert.Empty(found.PlatformIds);
  }
}
