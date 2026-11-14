using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisServiceListTests : HypothesisServiceFixture
{
  [Fact]
  public async Task ListAsync_OrdersByStatusThenName()
  {
    await Service.CreateAsync(Input(name: "z disabled", status: HypothesisStatus.Disabled));
    await Service.CreateAsync(Input(name: "paused", status: HypothesisStatus.Paused));
    await Service.CreateAsync(Input(name: "draft b", status: HypothesisStatus.Draft));
    await Service.CreateAsync(Input(name: "Draft a", status: HypothesisStatus.Draft));
    await Service.CreateAsync(Input(name: "expanded", status: HypothesisStatus.Expanded));
    await Service.CreateAsync(Input(name: "b active", status: HypothesisStatus.Active));
    await Service.CreateAsync(Input(name: "A active", status: HypothesisStatus.Active));

    var names = (await Service.ListAsync()).Select(h => h.Name).ToList();

    Assert.Equal(["A active", "b active", "expanded", "Draft a", "draft b", "paused", "z disabled"], names);
  }

  [Fact]
  public async Task ListAsync_ReturnsOnlyCurrentUsersHypotheses()
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
    var created = await Service.CreateAsync(Input(workFormat: WorkFormat.Hybrid, languageId: SeedData.EnglishLanguageId, status: HypothesisStatus.Paused));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(created.Id, item.Id);
    Assert.Equal("Unity Developer", item.Role);
    Assert.Equal(WorkFormat.Hybrid, item.WorkFormat);
    Assert.Equal("en", item.LanguageCode);
    Assert.Equal(HypothesisStatus.Paused, item.Status);
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
