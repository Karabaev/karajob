using Karajob.Application.Common;
using Karajob.Application.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Cvs;

public class CvServiceTagTests : CvServiceFixture
{
  private async Task<List<string>> TagNamesAsync(Guid userId)
  {
    await using var context = Database.CreateContext(userId);
    return await context.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync();
  }

  [Fact]
  public async Task CreateAsync_NewTagNames_CreatesLowercaseTagsForCurrentUser()
  {
    await Service.CreateAsync(Input(tags: ["Unity", " C# "]), Upload());

    Assert.Equal(["c#", "unity"], await TagNamesAsync(SeedData.OwnerId));
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.All(await context.Tags.ToListAsync(), t => Assert.Equal(SeedData.OwnerId, t.UserId));
  }

  [Fact]
  public async Task CreateAsync_DuplicateNamesInDifferentCase_CreatesSingleTagRow()
  {
    await Service.CreateAsync(Input(tags: ["Unity", "UNITY", " unity "]), Upload());

    Assert.Equal(["unity"], await TagNamesAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task CreateAsync_Tags_ReturnsTagItemsSortedByName()
  {
    var details = await Service.CreateAsync(Input(tags: ["zeta", "Alpha", "mid"]), Upload());

    Assert.Equal(["alpha", "mid", "zeta"], details.Tags.Select(t => t.Name));
    Assert.All(details.Tags, t =>
    {
      Assert.NotEqual(Guid.Empty, t.Id);
      Assert.Null(t.Color);
    });
  }

  [Fact]
  public async Task CreateAsync_Tags_PersistsLinksOnDocument()
  {
    var details = await Service.CreateAsync(Input(tags: ["unity", "games"]), Upload());

    var reloaded = await Service.FindAsync(details.Id);
    Assert.Equal(["games", "unity"], reloaded!.Tags.Select(t => t.Name));
    Assert.Equal(details.Tags.Select(t => t.Id).Order(), reloaded.Tags.Select(t => t.Id).Order());
  }

  [Fact]
  public async Task CreateAsync_ExistingTagInDifferentCase_ReusesSameTagAndCreatesNoDuplicate()
  {
    var existing = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");

    var details = await Service.CreateAsync(Input(tags: ["UNITY"]), Upload());

    Assert.Equal(existing.Id, Assert.Single(details.Tags).Id);
    Assert.Equal(["unity"], await TagNamesAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task CreateAsync_ExistingTagWithColor_KeepsColor()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");

    var details = await Service.CreateAsync(Input(tags: ["unity"]), Upload());

    Assert.Equal("#112233", Assert.Single(details.Tags).Color);
  }

  [Fact]
  public async Task CreateAsync_SameTagOnTwoCvs_SharesSingleTagRow()
  {
    var first = await Service.CreateAsync(Input("First", tags: ["Unity"]), Upload());
    var second = await Service.CreateAsync(Input("Second", tags: ["unity", "games"]), Upload());

    var shared = Assert.Single(first.Tags);
    Assert.Equal(shared.Id, second.Tags.Single(t => t.Name == "unity").Id);
    Assert.Equal(["games", "unity"], await TagNamesAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task CreateAsync_OtherUserHasSameTagName_CreatesSeparateTagForCurrentUser()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await PersistenceFixtures.AddTagAsync(Database, other.Id, "unity", "#aabbcc");

    var details = await Service.CreateAsync(Input(tags: ["unity"]), Upload());

    var tag = Assert.Single(details.Tags);
    Assert.NotEqual(foreign.Id, tag.Id);
    Assert.Null(tag.Color);
    Assert.Equal(["unity"], await TagNamesAsync(SeedData.OwnerId));
    Assert.Equal(["unity"], await TagNamesAsync(other.Id));
  }

  [Fact]
  public async Task CreateAsync_TwoUsersSameTagName_EachListSeesOnlyOwnTag()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var mine = await Service.CreateAsync(Input(tags: ["unity"]), Upload());
    var theirs = await ServiceFor(other.Id).CreateAsync(Input(tags: ["unity"]), Upload());

    Assert.NotEqual(mine.Tags[0].Id, theirs.Tags[0].Id);
    Assert.Equal(mine.Tags[0].Id, Assert.Single(Assert.Single(await Service.ListAsync()).Tags).Id);
    Assert.Equal(theirs.Tags[0].Id, Assert.Single(Assert.Single(await ServiceFor(other.Id).ListAsync()).Tags).Id);
  }

  [Fact]
  public async Task CreateAsync_InvalidFileWithTags_CreatesNoTags()
  {
    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(tags: ["unity"]), Upload("notes.txt")));

    Assert.Empty(await TagNamesAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task ListAsync_TagsOnCv_ReturnsIdNameAndColor()
  {
    var tag = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", tag);

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(new TagItem(tag.Id, "unity", "#112233"), Assert.Single(item.Tags));
  }

  [Fact]
  public async Task FindAsync_TagWithColor_ReturnsColor()
  {
    var tag = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", tag);

    var details = await Service.FindAsync(document.Id);

    Assert.Equal(new TagItem(tag.Id, "unity", "#112233"), Assert.Single(details!.Tags));
  }

  [Fact]
  public async Task UpdateAsync_ReplacesTagSet_KeepingSharedTagId()
  {
    var created = await Service.CreateAsync(Input(tags: ["unity", "games"]), Upload());
    var unityId = created.Tags.Single(t => t.Name == "unity").Id;

    var updated = await Service.UpdateAsync(created.Id, Input(tags: ["Unity", "unreal"]));

    Assert.Equal(["unity", "unreal"], updated.Tags.Select(t => t.Name));
    Assert.Equal(unityId, updated.Tags.Single(t => t.Name == "unity").Id);
    var reloaded = await Service.FindAsync(created.Id);
    Assert.Equal(["unity", "unreal"], reloaded!.Tags.Select(t => t.Name));
  }

  [Fact]
  public async Task UpdateAsync_NewTagName_CreatesTag()
  {
    var created = await Service.CreateAsync(Input(), Upload());

    await Service.UpdateAsync(created.Id, Input(tags: ["Fresh"]));

    Assert.Equal(["fresh"], await TagNamesAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task UpdateAsync_EmptyTags_RemovesAllLinks()
  {
    var tag = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", tag);

    var updated = await Service.UpdateAsync(document.Id, Input("Main"));

    Assert.Empty(updated.Tags);
    var reloaded = await Service.FindAsync(document.Id);
    Assert.Empty(reloaded!.Tags);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.CvDocumentTags.ToListAsync());
  }

  [Fact]
  public async Task UpdateAsync_TagRemovedFromOneCv_StaysOnOtherCv()
  {
    var first = await Service.CreateAsync(Input("First", tags: ["unity"]), Upload());
    var second = await Service.CreateAsync(Input("Second", tags: ["unity"]), Upload());

    await Service.UpdateAsync(first.Id, Input("First"));

    var other = await Service.FindAsync(second.Id);
    Assert.Equal(["unity"], other!.Tags.Select(t => t.Name));
  }

  [Fact]
  public async Task UpdateAsync_SameTagsTwice_DoesNotDuplicateLinks()
  {
    var created = await Service.CreateAsync(Input(tags: ["unity"]), Upload());

    await Service.UpdateAsync(created.Id, Input(tags: ["unity"]));
    await Service.UpdateAsync(created.Id, Input(tags: ["unity"]));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Single(await context.CvDocumentTags.ToListAsync());
  }

  [Fact]
  public async Task UpdateAsync_OtherUsersTagName_UsesOwnTagNotForeignOne()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await PersistenceFixtures.AddTagAsync(Database, other.Id, "unity");
    var created = await Service.CreateAsync(Input(), Upload());

    var updated = await Service.UpdateAsync(created.Id, Input(tags: ["unity"]));

    Assert.NotEqual(foreign.Id, Assert.Single(updated.Tags).Id);
  }

  [Fact]
  public async Task DeleteAsync_CvWithTags_RemovesLinksButKeepsTags()
  {
    var tag = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", tag);

    await Service.DeleteAsync(document.Id);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.CvDocumentTags.ToListAsync());
    Assert.Equal(tag.Id, Assert.Single(await context.Tags.ToListAsync()).Id);
  }
}
