using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Cvs;

public class CvServiceDeleteTests : CvServiceFixture
{
  [Fact]
  public async Task DeleteVersionAsync_NotLast_RemovesVersionRowAndStoredFile()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));
    Clock.UtcNow = Now.AddDays(1);
    var withTwo = await Service.AddVersionAsync(created.Id, Upload("v2.pdf"));
    var toDelete = withTwo.Versions.Single(v => v.FileName == "v1.pdf");

    await Service.DeleteVersionAsync(toDelete.Id);

    var details = await Service.FindAsync(created.Id);
    Assert.Equal(["v2.pdf"], details!.Versions.Select(v => v.FileName));
    Assert.Single(Storage.Paths);
    Assert.Null(await Service.OpenVersionAsync(toDelete.Id));
  }

  [Fact]
  public async Task DeleteVersionAsync_LastVersion_ThrowsValidationWithEmptyKeyAndKeepsEverything()
  {
    var created = await Service.CreateAsync(Input(), Upload());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteVersionAsync(created.Versions[0].Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Single(Storage.Paths);
    Assert.Equal(1, await CountVersionsAsync());
  }

  [Fact]
  public async Task DeleteVersionAsync_ReferencedByApplication_ThrowsValidationWithEmptyKeyAndKeepsEverything()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));
    Clock.UtcNow = Now.AddDays(1);
    var withTwo = await Service.AddVersionAsync(created.Id, Upload("v2.pdf"));
    var referenced = withTwo.Versions.Single(v => v.FileName == "v1.pdf");
    await AddApplicationAsync(SeedData.OwnerId, referenced.Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteVersionAsync(referenced.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Equal(2, Storage.Paths.Count);
    Assert.Equal(2, await CountVersionsAsync());
  }

  [Fact]
  public async Task DeleteVersionAsync_Unknown_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteVersionAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteVersionAsync_VersionOfAnotherUser_ThrowsKeyNotFoundAndKeepsIt()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherService = ServiceFor(other.Id);
    var foreign = await otherService.CreateAsync(Input(), Upload("v1.pdf"));
    await otherService.AddVersionAsync(foreign.Id, Upload("v2.pdf"));

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteVersionAsync(foreign.Versions[0].Id));

    Assert.Equal(2, Storage.Paths.Count);
  }

  [Fact]
  public async Task DeleteAsync_Unreferenced_RemovesDocumentVersionsAndAllFiles()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));
    Clock.UtcNow = Now.AddDays(1);
    await Service.AddVersionAsync(created.Id, Upload("v2.pdf"));

    await Service.DeleteAsync(created.Id);

    Assert.Null(await Service.FindAsync(created.Id));
    Assert.Empty(Storage.Paths);
    Assert.Equal(0, await CountDocumentsAsync());
    Assert.Equal(0, await CountVersionsAsync());
  }

  [Fact]
  public async Task DeleteAsync_DoesNotTouchOtherDocuments()
  {
    var first = await Service.CreateAsync(Input("A"), Upload());
    var second = await Service.CreateAsync(Input("B"), Upload());

    await Service.DeleteAsync(first.Id);

    Assert.NotNull(await Service.FindAsync(second.Id));
    Assert.Single(Storage.Paths);
  }

  [Fact]
  public async Task DeleteAsync_VersionReferencedByApplication_ThrowsValidationWithEmptyKeyAndKeepsEverything()
  {
    var created = await Service.CreateAsync(Input(), Upload());
    await AddApplicationAsync(SeedData.OwnerId, created.Versions[0].Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
    Assert.Single(Storage.Paths);
  }

  [Fact]
  public async Task DeleteAsync_PrimaryCvOfHypothesis_ThrowsValidationWithEmptyKeyAndKeepsEverything()
  {
    var created = await Service.CreateAsync(Input(), Upload());
    await AddHypothesisWithPrimaryCvAsync(SeedData.OwnerId, created.Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
    Assert.Single(Storage.Paths);
  }

  [Fact]
  public async Task DeleteAsync_Unknown_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_DocumentOfAnotherUser_ThrowsKeyNotFoundAndKeepsIt()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherService = ServiceFor(other.Id);
    var foreign = await otherService.CreateAsync(Input(), Upload());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(foreign.Id));

    Assert.NotNull(await otherService.FindAsync(foreign.Id));
    Assert.Single(Storage.Paths);
  }

  [Fact]
  public async Task DeleteVersionAsync_FileDeleteFails_StillRemovesVersion()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));
    Clock.UtcNow = Now.AddDays(1);
    var withTwo = await Service.AddVersionAsync(created.Id, Upload("v2.pdf"));
    Storage.DeleteFailure = new IOException("File is in use");

    await Service.DeleteVersionAsync(withTwo.Versions.Single(v => v.FileName == "v1.pdf").Id);

    Assert.Equal(1, await CountVersionsAsync());
  }

  [Fact]
  public async Task DeleteAsync_FileDeleteFails_StillRemovesDocument()
  {
    var created = await Service.CreateAsync(Input(), Upload());
    Storage.DeleteFailure = new UnauthorizedAccessException();

    await Service.DeleteAsync(created.Id);

    Assert.Equal(0, await CountDocumentsAsync());
  }
}
