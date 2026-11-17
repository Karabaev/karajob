using Karajob.Domain.Cvs;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Tracks;

public class TrackServiceListTests : TrackServiceFixture
{
  [Fact]
  public async Task ListAsync_OrdersByStatusThenName()
  {
    await CreateWithStatusAsync("z disabled", TrackStatus.Disabled);
    await Service.CreateAsync(Input(name: "paused", status: TrackStatus.Paused));
    await Service.CreateAsync(Input(name: "draft b", status: TrackStatus.Draft));
    await Service.CreateAsync(Input(name: "Draft a", status: TrackStatus.Draft));
    await CreateWithStatusAsync("expanded", TrackStatus.Expanded);
    await Service.CreateAsync(Input(name: "b active", status: TrackStatus.Active));
    await Service.CreateAsync(Input(name: "A active", status: TrackStatus.Active));

    var names = (await Service.ListAsync()).Select(h => h.Name).ToList();

    Assert.Equal(["A active", "b active", "expanded", "Draft a", "draft b", "paused", "z disabled"], names);
  }

  [Fact]
  public async Task ListAsync_ReturnsOnlyCurrentUsersTracks()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(other.Id).CreateAsync(Input(name: "Foreign"));
    await Service.CreateAsync(Input(name: "Mine"));

    var items = await Service.ListAsync();

    Assert.Equal(["Mine"], items.Select(h => h.Name));
  }

  [Fact]
  public async Task ListAsync_MapsListItemFields()
  {
    var created = await Service.CreateAsync(Input(workFormat: WorkFormat.Hybrid, languageId: SeedData.EnglishLanguageId, status: TrackStatus.Paused));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(created.Id, item.Id);
    Assert.Equal("Unity Developer", item.Role);
    Assert.Equal(WorkFormat.Hybrid, item.WorkFormat);
    Assert.Equal("en", item.LanguageCode);
    Assert.Equal(TrackStatus.Paused, item.Status);
  }

  [Fact]
  public async Task ListAsync_ReturnsTrackRoleId()
  {
    await Service.CreateAsync(Input(roleId: SeedData.UnrealDeveloperRoleId));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(SeedData.UnrealDeveloperRoleId, item.RoleId);
  }

  [Fact]
  public async Task ListAsync_ReturnsPlatformNamesSortedCaseInsensitively()
  {
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      var mobile = await context.Platforms.SingleAsync(p => p.Id == SeedData.MobilePlatformId);
      mobile.Rename("mobile");
      await context.SaveChangesAsync();
    }

    await Service.CreateAsync(Input(platformIds: [SeedData.WebPlatformId, SeedData.PcPlatformId, SeedData.MobilePlatformId, SeedData.VrPlatformId]));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(["mobile", "PC", "VR", "Web"], item.PlatformNames);
  }

  [Fact]
  public async Task ListAsync_TrackWithoutPlatforms_ReturnsEmptyPlatformNames()
  {
    await Service.CreateAsync(Input(platformIds: []));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Empty(item.PlatformNames);
  }

  [Fact]
  public async Task ListAsync_EachTrackGetsOnlyItsOwnPlatforms()
  {
    await Service.CreateAsync(Input(name: "A", platformIds: [SeedData.PcPlatformId, SeedData.WebPlatformId]));
    await Service.CreateAsync(Input(name: "B", platformIds: [SeedData.MobilePlatformId]));
    await Service.CreateAsync(Input(name: "C", platformIds: []));

    var items = (await Service.ListAsync()).ToDictionary(h => h.Name);

    Assert.Equal(["PC", "Web"], items["A"].PlatformNames);
    Assert.Equal(["Mobile"], items["B"].PlatformNames);
    Assert.Empty(items["C"].PlatformNames);
  }

  [Fact]
  public async Task ListCvOptionsAsync_ReturnsOwnCvsOrderedByName()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddCvAsync(Database, other.Id);
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      context.CvDocuments.Add(new CvDocument(SeedData.OwnerId, "b CV", SeedData.EnglishLanguageId, Now));
      context.CvDocuments.Add(new CvDocument(SeedData.OwnerId, "A CV", SeedData.RussianLanguageId, Now));
      await context.SaveChangesAsync();
    }

    var options = await Service.ListCvOptionsAsync();

    Assert.Equal(["A CV", "b CV"], options.Select(o => o.Name));
    Assert.Equal(["ru", "en"], options.Select(o => o.LanguageCode));
  }

  [Fact]
  public async Task ListCvOptionsAsync_NoCvs_ReturnsEmpty()
  {
    Assert.Empty(await Service.ListCvOptionsAsync());
  }
}
