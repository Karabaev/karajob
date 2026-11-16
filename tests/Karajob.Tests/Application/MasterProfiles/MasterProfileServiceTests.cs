using Karajob.Application.Common;
using Karajob.Application.MasterProfiles;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.MasterProfiles;

public class MasterProfileServiceTests : MasterProfileServiceFixture
{
  private async Task<(int Profiles, int Versions)> CountAsync(Guid userId)
  {
    await using var context = Database.CreateContext(userId);
    return (await context.MasterProfiles.CountAsync(), await context.MasterProfileVersions.CountAsync());
  }

  [Fact]
  public async Task GetAsync_NoProfile_CreatesFromTemplate()
  {
    var details = await Service.GetAsync();

    Assert.Equal(MasterProfileTestTemplate.English, details.Markdown);
    var version = Assert.Single(details.Versions);
    Assert.Equal(version.Id, details.CurrentVersionId);
    Assert.Equal(Now, version.CreatedAt);
    Assert.Null(version.Comment);
    Assert.Null(version.RestoredFromVersionId);
  }

  [Fact]
  public async Task GetAsync_CalledTwice_DoesNotCreateSecondProfileOrVersion()
  {
    var first = await Service.GetAsync();
    var second = await Service.GetAsync();

    Assert.Equal(first.CurrentVersionId, second.CurrentVersionId);
    Assert.Equal((1, 1), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task SaveAsync_Valid_AddsVersionNewestFirstWithTrimmedComment()
  {
    await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);

    var details = await Service.SaveAsync(new MasterProfileInput("# New text", " comment "));

    Assert.Equal("# New text", details.Markdown);
    Assert.Equal(2, details.Versions.Count);
    Assert.Equal("comment", details.Versions[0].Comment);
    Assert.Equal(Now.AddDays(1), details.Versions[0].CreatedAt);
    Assert.Equal(details.Versions[0].Id, details.CurrentVersionId);
    Assert.Equal(Now, details.Versions[1].CreatedAt);
  }

  [Fact]
  public async Task SaveAsync_MarkdownWithSurroundingWhitespace_StoresVerbatim()
  {
    await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);

    var details = await Service.SaveAsync(new MasterProfileInput("\n# Text\n\n", null));

    Assert.Equal("\n# Text\n\n", details.Markdown);
    var reloaded = await Service.GetVersionAsync(details.CurrentVersionId);
    Assert.Equal("\n# Text\n\n", reloaded.Markdown);
  }

  [Fact]
  public async Task SaveAsync_NoPriorGet_CreatesTemplateVersionThenSavedVersion()
  {
    Clock.UtcNow = Now.AddDays(1);

    var details = await Service.SaveAsync(new MasterProfileInput("# Mine", null));

    Assert.Equal("# Mine", details.Markdown);
    Assert.Equal(2, details.Versions.Count);
    var older = await Service.GetVersionAsync(details.Versions[1].Id);
    Assert.Equal(MasterProfileTestTemplate.English, older.Markdown);
    Assert.Equal((1, 2), await CountAsync(SeedData.OwnerId));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   \n ")]
  public async Task SaveAsync_BlankMarkdown_ThrowsValidationKeyedByMarkdownAndSavesNothing(string markdown)
  {
    await Service.GetAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.SaveAsync(new MasterProfileInput(markdown, null)));

    Assert.Contains(nameof(MasterProfileInput.Markdown), exception.Errors.Keys);
    Assert.Equal((1, 1), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task SaveAsync_SameAsCurrent_ThrowsValidationKeyedByMarkdownAndAddsNoVersion()
  {
    var current = await Service.GetAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.SaveAsync(new MasterProfileInput(current.Markdown, "comment")));

    Assert.Contains(nameof(MasterProfileInput.Markdown), exception.Errors.Keys);
    Assert.Equal((1, 1), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task GetVersionAsync_OlderVersion_ReturnsFullData()
  {
    var first = await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);
    var saved = await Service.SaveAsync(new MasterProfileInput("# Second", "second"));

    var older = await Service.GetVersionAsync(first.CurrentVersionId);
    var newer = await Service.GetVersionAsync(saved.CurrentVersionId);

    Assert.Equal(MasterProfileTestTemplate.English, older.Markdown);
    Assert.Equal(Now, older.CreatedAt);
    Assert.Null(older.Comment);
    Assert.Equal("# Second", newer.Markdown);
    Assert.Equal("second", newer.Comment);
    Assert.Equal(Now.AddDays(1), newer.CreatedAt);
  }

  [Fact]
  public async Task GetVersionAsync_UnknownId_ThrowsKeyNotFound()
  {
    await Service.GetAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetVersionAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task GetVersionAsync_VersionOfAnotherUser_ThrowsKeyNotFound()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherDetails = await ServiceFor(other.Id).GetAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetVersionAsync(otherDetails.CurrentVersionId));
  }

  [Fact]
  public async Task RestoreAsync_OlderVersion_CreatesNewNewestVersionWithOldText()
  {
    var first = await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);
    await Service.SaveAsync(new MasterProfileInput("# Second", "second"));
    Clock.UtcNow = Now.AddDays(2);

    var details = await Service.RestoreAsync(first.CurrentVersionId);

    Assert.Equal(MasterProfileTestTemplate.English, details.Markdown);
    Assert.Equal(3, details.Versions.Count);
    Assert.Equal(details.Versions[0].Id, details.CurrentVersionId);
    Assert.Equal(first.CurrentVersionId, details.Versions[0].RestoredFromVersionId);
    Assert.Null(details.Versions[0].Comment);
    Assert.Equal(Now.AddDays(2), details.Versions[0].CreatedAt);
    Assert.Contains(details.Versions, v => v.Id == first.CurrentVersionId);
    Assert.Equal((1, 3), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task RestoreAsync_CurrentVersion_ThrowsValidationAndAddsNoVersion()
  {
    var current = await Service.GetAsync();

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.RestoreAsync(current.CurrentVersionId));

    Assert.Equal((1, 1), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task RestoreAsync_OlderVersionWithTextEqualToCurrent_ThrowsValidationAndAddsNoVersion()
  {
    var first = await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);
    await Service.SaveAsync(new MasterProfileInput("# Second", null));
    Clock.UtcNow = Now.AddDays(2);
    await Service.SaveAsync(new MasterProfileInput(MasterProfileTestTemplate.English, null));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.RestoreAsync(first.CurrentVersionId));

    Assert.Equal((1, 3), await CountAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task RestoreAsync_UnknownId_ThrowsKeyNotFound()
  {
    await Service.GetAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RestoreAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task RestoreAsync_VersionOfAnotherUser_ThrowsKeyNotFoundAndAddsNothing()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherDetails = await ServiceFor(other.Id).GetAsync();
    await Service.GetAsync();
    await Service.SaveAsync(new MasterProfileInput("# Mine", null));

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RestoreAsync(otherDetails.CurrentVersionId));

    Assert.Equal((1, 2), await CountAsync(SeedData.OwnerId));
    Assert.Equal((1, 1), await CountAsync(other.Id));
  }

  [Fact]
  public async Task GetAsync_SecondUser_GetsOwnTemplateProfileIsolatedFromOwner()
  {
    await Service.GetAsync();
    Clock.UtcNow = Now.AddDays(1);
    await Service.SaveAsync(new MasterProfileInput("# Owner only", "owner"));
    var other = await PersistenceFixtures.AddUserAsync(Database);

    var details = await ServiceFor(other.Id).GetAsync();

    Assert.Equal(MasterProfileTestTemplate.English, details.Markdown);
    Assert.Single(details.Versions);
    Assert.DoesNotContain(details.Versions, v => v.Comment == "owner");
    Assert.Equal((1, 2), await CountAsync(SeedData.OwnerId));
    Assert.Equal((1, 1), await CountAsync(other.Id));
  }
}
