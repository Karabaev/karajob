using Karajob.Application.Common;
using Karajob.Application.Cvs;
using Karajob.Domain.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Cvs;

public class TagServiceTests : CvServiceFixture
{
  private TagService Tags => TagServiceFor(SeedData.OwnerId);

  private Task<Tag> AddTagAsync(string name, string? color = null, Guid? userId = null)
  {
    return PersistenceFixtures.AddTagAsync(Database, userId ?? SeedData.OwnerId, name, color);
  }

  [Fact]
  public async Task ListAsync_NoTags_ReturnsEmpty()
  {
    Assert.Empty(await Tags.ListAsync());
  }

  [Fact]
  public async Task ListAsync_Tags_ReturnsIdNameColorSortedByName()
  {
    var unity = await AddTagAsync("unity", "#112233");
    var games = await AddTagAsync("games");
    var csharp = await AddTagAsync("c#");

    var items = await Tags.ListAsync();

    Assert.Equal(
      [
        new TagListItem(csharp.Id, "c#", null, 0),
        new TagListItem(games.Id, "games", null, 0),
        new TagListItem(unity.Id, "unity", "#112233", 0),
      ],
      items);
  }

  [Fact]
  public async Task ListAsync_TagUsedByTwoCvs_CountsEachCvOnce()
  {
    var unity = await AddTagAsync("unity");
    var games = await AddTagAsync("games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "One", unity, games);
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Two", unity);

    var items = await Tags.ListAsync();

    Assert.Equal(1, items.Single(i => i.Name == "games").CvCount);
    Assert.Equal(2, items.Single(i => i.Name == "unity").CvCount);
  }

  [Fact]
  public async Task ListAsync_UnusedTag_IsListedWithZeroCount()
  {
    await AddTagAsync("lonely");

    Assert.Equal(0, Assert.Single(await Tags.ListAsync()).CvCount);
  }

  [Fact]
  public async Task ListAsync_TagsOfAnotherUser_AreNotListedNorCounted()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await AddTagAsync("unity", userId: other.Id);
    await PersistenceFixtures.AddCvWithTagsAsync(Database, other.Id, "Foreign", foreign);
    var mine = await AddTagAsync("games");

    var items = await Tags.ListAsync();

    Assert.Equal(mine.Id, Assert.Single(items).Id);
  }

  [Fact]
  public async Task SearchAsync_Prefix_ReturnsOnlyTagsStartingWithIt()
  {
    await AddTagAsync("unity");
    await AddTagAsync("unreal");
    await AddTagAsync("games");
    await AddTagAsync("community");

    var items = await Tags.SearchAsync("un", 10);

    Assert.Equal(["unity", "unreal"], items.Select(i => i.Name));
  }

  [Fact]
  public async Task SearchAsync_PrefixInDifferentCaseWithSpaces_IsNormalized()
  {
    await AddTagAsync("unity");
    await AddTagAsync("games");

    var items = await Tags.SearchAsync("  UNI ", 10);

    Assert.Equal(["unity"], items.Select(i => i.Name));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task SearchAsync_NullOrBlankPrefix_ReturnsAllSortedByName(string? prefix)
  {
    await AddTagAsync("unity");
    await AddTagAsync("games");
    await AddTagAsync("c#");

    var items = await Tags.SearchAsync(prefix, 10);

    Assert.Equal(["c#", "games", "unity"], items.Select(i => i.Name));
  }

  [Fact]
  public async Task SearchAsync_Limit_CapsResultsKeepingNameOrder()
  {
    await AddTagAsync("d");
    await AddTagAsync("b");
    await AddTagAsync("a");
    await AddTagAsync("c");

    var items = await Tags.SearchAsync(null, 2);

    Assert.Equal(["a", "b"], items.Select(i => i.Name));
  }

  [Fact]
  public async Task SearchAsync_ReturnsIdAndColor()
  {
    var tag = await AddTagAsync("unity", "#112233");

    var item = Assert.Single(await Tags.SearchAsync("un", 5));

    Assert.Equal(new TagItem(tag.Id, "unity", "#112233"), item);
  }

  [Fact]
  public async Task SearchAsync_NoMatch_ReturnsEmpty()
  {
    await AddTagAsync("unity");

    Assert.Empty(await Tags.SearchAsync("zzz", 10));
  }

  [Fact]
  public async Task SearchAsync_TagsOfAnotherUser_AreNotReturned()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddTagAsync("unity", userId: other.Id);

    Assert.Empty(await Tags.SearchAsync("un", 10));
    Assert.Empty(await Tags.SearchAsync(null, 10));
  }

  [Fact]
  public async Task SetColorAsync_ValidColor_PersistsIt()
  {
    var tag = await AddTagAsync("unity");

    await Tags.SetColorAsync(tag.Id, "#a1b2c3");

    Assert.Equal("#a1b2c3", Assert.Single(await Tags.ListAsync()).Color);
  }

  [Fact]
  public async Task SetColorAsync_UppercaseColor_StoresLowercase()
  {
    var tag = await AddTagAsync("unity");

    await Tags.SetColorAsync(tag.Id, "#A1B2C3");

    Assert.Equal("#a1b2c3", Assert.Single(await Tags.ListAsync()).Color);
  }

  [Fact]
  public async Task SetColorAsync_Null_ClearsColor()
  {
    var tag = await AddTagAsync("unity", "#112233");

    await Tags.SetColorAsync(tag.Id, null);

    Assert.Null(Assert.Single(await Tags.ListAsync()).Color);
  }

  [Theory]
  [InlineData("red")]
  [InlineData("#fff")]
  [InlineData("a1b2c3")]
  [InlineData("#12345g")]
  public async Task SetColorAsync_InvalidColor_ThrowsValidationKeyedByColorAndKeepsPrevious(string color)
  {
    var tag = await AddTagAsync("unity", "#112233");

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Tags.SetColorAsync(tag.Id, color));

    Assert.Contains("Color", exception.Errors.Keys);
    Assert.Equal("#112233", Assert.Single(await Tags.ListAsync()).Color);
  }

  [Fact]
  public async Task SetColorAsync_UnknownTag_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Tags.SetColorAsync(Guid.NewGuid(), "#112233"));
  }

  [Fact]
  public async Task SetColorAsync_TagOfAnotherUser_ThrowsKeyNotFoundAndKeepsColor()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await AddTagAsync("unity", "#112233", other.Id);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Tags.SetColorAsync(foreign.Id, "#ffffff"));

    var items = await TagServiceFor(other.Id).ListAsync();
    Assert.Equal("#112233", Assert.Single(items).Color);
  }

  [Fact]
  public async Task SetColorAsync_Valid_ShowsOnCvTagItems()
  {
    var tag = await AddTagAsync("unity");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", tag);

    await Tags.SetColorAsync(tag.Id, "#a1b2c3");

    var item = Assert.Single(await Service.ListAsync());
    Assert.Equal("#a1b2c3", Assert.Single(item.Tags).Color);
  }

  [Fact]
  public async Task DeleteAsync_Tag_RemovesItFromList()
  {
    var tag = await AddTagAsync("unity");
    var keep = await AddTagAsync("games");

    await Tags.DeleteAsync(tag.Id);

    Assert.Equal(keep.Id, Assert.Single(await Tags.ListAsync()).Id);
  }

  [Fact]
  public async Task DeleteAsync_TagUsedByCvs_DetachesTagAndKeepsCvs()
  {
    var unity = await AddTagAsync("unity");
    var games = await AddTagAsync("games");
    var first = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "One", unity, games);
    var second = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Two", unity);

    await Tags.DeleteAsync(unity.Id);

    var items = await Service.ListAsync();
    Assert.Equal(2, items.Count);
    Assert.Equal(["games"], items.Single(i => i.Id == first.Id).Tags.Select(t => t.Name));
    Assert.Empty(items.Single(i => i.Id == second.Id).Tags);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.CvDocuments.CountAsync());
    Assert.DoesNotContain(await context.CvDocumentTags.ToListAsync(), l => l.TagId == unity.Id);
  }

  [Fact]
  public async Task DeleteAsync_Unknown_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Tags.DeleteAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_TagOfAnotherUser_ThrowsKeyNotFoundAndKeepsIt()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await AddTagAsync("unity", userId: other.Id);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Tags.DeleteAsync(foreign.Id));

    Assert.Single(await TagServiceFor(other.Id).ListAsync());
  }

  [Fact]
  public async Task DeleteAsync_ThenCreateCvWithSameName_CreatesFreshTag()
  {
    var tag = await AddTagAsync("unity", "#112233");
    await Tags.DeleteAsync(tag.Id);

    var details = await Service.CreateAsync(Input(tags: ["unity"]), Upload());

    var recreated = Assert.Single(details.Tags);
    Assert.NotEqual(tag.Id, recreated.Id);
    Assert.Null(recreated.Color);
  }
}
