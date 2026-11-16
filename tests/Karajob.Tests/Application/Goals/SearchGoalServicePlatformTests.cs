using Karajob.Application.Common;
using Karajob.Application.Goals;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Goals;

public class SearchGoalServicePlatformTests : SearchGoalServiceFixture
{
  [Fact]
  public async Task GetAsync_SeededOwner_HasNoPlatforms()
  {
    Assert.Empty((await Service.GetAsync()).PlatformIds);
  }

  [Fact]
  public async Task UpdateAsync_WithPlatforms_PersistsThem()
  {
    await Service.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.VrPlatformId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.VrPlatformId }.Order(), details.PlatformIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_ReturnedDetails_ContainPlatforms()
  {
    var returned = await Service.UpdateAsync(Input(platformIds: [SeedData.WebPlatformId]));

    Assert.Equal([SeedData.WebPlatformId], returned.PlatformIds);
  }

  [Fact]
  public async Task UpdateAsync_DuplicatePlatformIds_StoredOnce()
  {
    await Service.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.PcPlatformId, SeedData.WebPlatformId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.WebPlatformId }.Order(), details.PlatformIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.SearchGoalPlatforms.CountAsync());
  }

  [Fact]
  public async Task UpdateAsync_SecondUpdateWithOtherPlatforms_ReplacesPlatforms()
  {
    await Service.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId, SeedData.VrPlatformId]));

    await Service.UpdateAsync(Input(platformIds: [SeedData.VrPlatformId, SeedData.MobilePlatformId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.VrPlatformId, SeedData.MobilePlatformId }.Order(), details.PlatformIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_EmptyPlatforms_ClearsPlatforms()
  {
    await Service.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId]));

    await Service.UpdateAsync(Input(platformIds: []));

    Assert.Empty((await Service.GetAsync()).PlatformIds);
  }

  [Fact]
  public async Task UpdateAsync_UnknownPlatform_FailsOnPlatformIdsWithMessage()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId, Guid.NewGuid()])));

    Assert.Equal("Платформа не найдена", exception.Errors[nameof(SearchGoalInput.PlatformIds)]);
    Assert.Empty((await Service.GetAsync()).PlatformIds);
  }

  [Fact]
  public async Task UpdateAsync_OtherProfile_DoesNotChangeFirstProfilePlatforms()
  {
    var second = ServiceFor(await CreateProfileAsync());

    await second.UpdateAsync(Input(platformIds: [SeedData.PcPlatformId]));

    Assert.Empty((await Service.GetAsync()).PlatformIds);
    Assert.Equal([SeedData.PcPlatformId], (await second.GetAsync()).PlatformIds);
  }
}
