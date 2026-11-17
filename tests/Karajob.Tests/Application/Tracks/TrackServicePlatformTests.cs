using Karajob.Application.Common;
using Karajob.Application.Tracks;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Tracks;

public class TrackServicePlatformTests : TrackServiceFixture
{
  [Fact]
  public async Task CreateAsync_WithPlatforms_PersistsThemAndFindReturnsThem()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.VrPlatformId]));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.VrPlatformId }.Order(), found.PlatformIds.Order());
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
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.PcPlatformId, SeedData.WebPlatformId]));

    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.WebPlatformId }.Order(), created.PlatformIds.Order());
  }

  [Fact]
  public async Task CreateAsync_UnknownPlatformId_ThrowsValidationWithKeyAndMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, Guid.NewGuid()])));

    Assert.Equal("Platform not found", exception.Errors[nameof(TrackInput.PlatformIds)]);
    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public async Task UpdateAsync_ReplacingPlatforms_RemovesOldOnes()
  {
    var created = await Service.CreateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.VrPlatformId]));

    await Service.UpdateAsync(created.Id, Input(platformIds: [SeedData.VrPlatformId, SeedData.MobilePlatformId]));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.VrPlatformId, SeedData.MobilePlatformId }.Order(), found!.PlatformIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.TrackPlatforms.CountAsync(p => p.TrackId == created.Id));
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

    Assert.Equal("Platform not found", exception.Errors[nameof(TrackInput.PlatformIds)]);
    Assert.Equal([SeedData.PcPlatformId], (await Service.FindAsync(created.Id))!.PlatformIds);
  }
}
