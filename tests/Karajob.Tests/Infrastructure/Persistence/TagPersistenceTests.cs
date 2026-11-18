using Karajob.Domain.Cvs;
using Karajob.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class TagPersistenceTests
{
  [Fact]
  public async Task SaveChanges_SameNameForSameUser_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Tags.Add(new Tag(SeedData.OwnerId, "Unity"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameNameForDifferentUsers_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");

    await PersistenceFixtures.AddTagAsync(database, other.Id, "unity");

    await using var context = database.CreateContext(other.Id);
    Assert.Single(await context.Tags.ToListAsync());
  }

  [Fact]
  public async Task Tags_SavedForOtherUser_AreHiddenFromCurrentUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddTagAsync(database, other.Id, "unity");

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.Tags.ToListAsync());
    Assert.Single(await otherContext.Tags.ToListAsync());
  }

  [Fact]
  public async Task CvDocumentTags_SavedForOtherUser_AreHiddenFromCurrentUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    var tag = await PersistenceFixtures.AddTagAsync(database, other.Id, "unity");
    await PersistenceFixtures.AddCvWithTagsAsync(database, other.Id, "Foreign", tag);

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.CvDocumentTags.ToListAsync());
    Assert.Single(await otherContext.CvDocumentTags.ToListAsync());
  }

  [Fact]
  public async Task CvDocument_SavedWithTags_LoadsTagLinksAutomatically()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var first = await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");
    var second = await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "games");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(database, SeedData.OwnerId, "Main", first, second);

    await using var context = database.CreateContext(SeedData.OwnerId);
    var loaded = await context.CvDocuments.SingleAsync(d => d.Id == document.Id);

    Assert.Equal(new[] { first.Id, second.Id }.Order(), loaded.Tags.Select(t => t.TagId).Order());
  }

  [Fact]
  public async Task Insert_SameTagTwiceOnOneCv_ViolatesUniqueIndex()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var tag = await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(database, SeedData.OwnerId, "Main", tag);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var linkId = Guid.NewGuid();

    await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlInterpolatedAsync(
      $"INSERT INTO CvDocumentTags (Id, UserId, CvDocumentId, TagId) VALUES ({linkId}, {SeedData.OwnerId}, {document.Id}, {tag.Id})"));
  }

  [Fact]
  public async Task DeleteTag_LinkedToCvs_RemovesLinksAndKeepsCvs()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var tag = await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(database, SeedData.OwnerId, "Main", tag);

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Tags.Remove(await context.Tags.SingleAsync());
      await context.SaveChangesAsync();
    }

    await using var verify = database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await verify.CvDocumentTags.ToListAsync());
    Assert.Equal(document.Id, Assert.Single(await verify.CvDocuments.ToListAsync()).Id);
  }

  [Fact]
  public async Task DeleteCvDocument_WithTags_RemovesLinksAndKeepsTag()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var tag = await PersistenceFixtures.AddTagAsync(database, SeedData.OwnerId, "unity");
    await PersistenceFixtures.AddCvWithTagsAsync(database, SeedData.OwnerId, "Main", tag);

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.CvDocuments.Remove(await context.CvDocuments.SingleAsync());
      await context.SaveChangesAsync();
    }

    await using var verify = database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await verify.CvDocumentTags.ToListAsync());
    Assert.Equal(tag.Id, Assert.Single(await verify.Tags.ToListAsync()).Id);
  }
}
