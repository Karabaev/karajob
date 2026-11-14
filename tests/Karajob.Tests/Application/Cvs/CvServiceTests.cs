using Karajob.Application.Common;
using Karajob.Application.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Cvs;

public class CvServiceTests : CvServiceFixture
{
  private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

  [Fact]
  public async Task CreateAsync_Valid_CreatesDocumentWithSingleVersion()
  {
    var details = await Service.CreateAsync(Input("Main CV"), Upload("cv.pdf", comment: "first"));

    Assert.NotEqual(Guid.Empty, details.Id);
    Assert.Equal("Main CV", details.Name);
    Assert.Equal(SeedData.RussianLanguageId, details.LanguageId);
    var version = Assert.Single(details.Versions);
    Assert.Equal("cv.pdf", version.FileName);
    Assert.Equal(PdfBytes.Length, version.SizeBytes);
    Assert.Equal("first", version.Comment);
    Assert.Equal(Now, version.CreatedAt);
  }

  [Fact]
  public async Task CreateAsync_Valid_PersistsDocumentOwnedByCurrentUser()
  {
    var details = await Service.CreateAsync(Input(), Upload());

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var document = Assert.Single(context.CvDocuments.ToList());
    Assert.Equal(details.Id, document.Id);
    Assert.Equal(SeedData.OwnerId, document.UserId);
  }

  [Fact]
  public async Task CreateAsync_Valid_TrimsName()
  {
    var details = await Service.CreateAsync(Input("  Main CV  "), Upload());

    Assert.Equal("Main CV", details.Name);
  }

  [Fact]
  public async Task CreateAsync_Tags_TrimsDropsBlankAndDeduplicatesCaseInsensitivelyKeepingOrder()
  {
    var details = await Service.CreateAsync(
      Input(tags: ["  Unity ", "", "   ", "unity", "C#", "UNITY", "Games", "c#"]),
      Upload());

    Assert.Equal(["Unity", "C#", "Games"], details.Tags);
  }

  [Fact]
  public async Task CreateAsync_NoTags_ReturnsEmptyTags()
  {
    var details = await Service.CreateAsync(Input(), Upload());

    Assert.Empty(details.Tags);
  }

  [Fact]
  public async Task CreateAsync_Valid_SavesBytesUnderUserScopedLowerCasePath()
  {
    await Service.CreateAsync(Input(), Upload("cv.pdf", PdfBytes));

    var path = Assert.Single(Storage.Paths);
    Assert.StartsWith($"cv/{SeedData.OwnerId:N}/", path, StringComparison.Ordinal);
    Assert.Equal(path.ToLowerInvariant(), path);
    Assert.EndsWith(".pdf", path, StringComparison.Ordinal);
    Assert.Equal(PdfBytes, Storage.ReadAll(path));
  }

  [Fact]
  public async Task CreateAsync_Valid_StoresPathOnVersion()
  {
    await Service.CreateAsync(Input(), Upload());

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var version = Assert.Single(context.CvVersions.ToList());
    Assert.Equal(Assert.Single(Storage.Paths), version.StoragePath);
  }

  [Fact]
  public async Task CreateAsync_UpperCaseExtension_AcceptedAndStoredLowerCase()
  {
    await Service.CreateAsync(Input(), Upload("CV.PDF"));

    Assert.EndsWith(".pdf", Assert.Single(Storage.Paths), StringComparison.Ordinal);
  }

  [Fact]
  public async Task CreateAsync_TwoUploads_UseDifferentPaths()
  {
    await Service.CreateAsync(Input("A"), Upload("cv.pdf"));
    await Service.CreateAsync(Input("B"), Upload("cv.pdf"));

    Assert.Equal(2, Storage.Paths.Distinct().Count());
  }

  [Theory]
  [InlineData("C:\\Users\\me\\My CV.pdf")]
  [InlineData("dir/My CV.pdf")]
  [InlineData("My CV.pdf")]
  public async Task CreateAsync_FileNameWithDirectory_StoresBareFileName(string clientFileName)
  {
    var details = await Service.CreateAsync(Input(), Upload(clientFileName));

    Assert.Equal("My CV.pdf", Assert.Single(details.Versions).FileName);
  }

  [Fact]
  public async Task CreateAsync_Pdf_PersistsPdfContentType()
  {
    await Service.CreateAsync(Input(), Upload("cv.pdf"));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal("application/pdf", Assert.Single(context.CvVersions.ToList()).ContentType);
  }

  [Fact]
  public async Task CreateAsync_Docx_PersistsDocxContentType()
  {
    await Service.CreateAsync(Input(), Upload("cv.docx"));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(DocxContentType, Assert.Single(context.CvVersions.ToList()).ContentType);
    Assert.EndsWith(".docx", Assert.Single(Storage.Paths), StringComparison.Ordinal);
  }

  [Fact]
  public async Task CreateAsync_BlankComment_StoresNull()
  {
    var details = await Service.CreateAsync(Input(), Upload(comment: "   "));

    Assert.Null(Assert.Single(details.Versions).Comment);
  }

  [Fact]
  public async Task CreateAsync_Comment_IsTrimmed()
  {
    var details = await Service.CreateAsync(Input(), Upload(comment: "  v1  "));

    Assert.Equal("v1", Assert.Single(details.Versions).Comment);
  }

  [Fact]
  public async Task CreateAsync_ExactlyMaxSize_Accepted()
  {
    var details = await Service.CreateAsync(Input(), Upload(sizeBytes: 5L * 1024 * 1024));

    Assert.Single(details.Versions);
  }

  [Fact]
  public async Task CreateAsync_BlankName_ThrowsValidationKeyedByName()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input("   "), Upload()));

    Assert.Contains(nameof(CvInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NullLanguage_ThrowsValidationKeyedByLanguageId()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(NoLanguageInput(), Upload()));

    Assert.Contains(nameof(CvInput.LanguageId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownLanguage_ThrowsValidationKeyedByLanguageId()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(languageId: Guid.NewGuid()), Upload()));

    Assert.Contains(nameof(CvInput.LanguageId), exception.Errors.Keys);
  }

  [Theory]
  [InlineData("notes.txt")]
  [InlineData("old.doc")]
  [InlineData("noextension")]
  public async Task CreateAsync_UnsupportedExtension_ThrowsValidationKeyedByFile(string fileName)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(), Upload(fileName)));

    Assert.Contains(CvService.FileErrorKey, exception.Errors.Keys);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-5)]
  public async Task CreateAsync_NonPositiveSize_ThrowsValidationKeyedByFile(long size)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(), Upload(sizeBytes: size)));

    Assert.Contains(CvService.FileErrorKey, exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_OverMaxSize_ThrowsValidationKeyedByFile()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(), Upload(sizeBytes: 5L * 1024 * 1024 + 1)));

    Assert.Contains(CvService.FileErrorKey, exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NameLanguageAndFileInvalid_ReportsAllErrorsAtOnce()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(NoLanguageInput(" "), Upload("notes.txt")));

    Assert.Contains(nameof(CvInput.Name), exception.Errors.Keys);
    Assert.Contains(nameof(CvInput.LanguageId), exception.Errors.Keys);
    Assert.Contains(CvService.FileErrorKey, exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NameInvalid_DoesNotOpenStreamNorStoreAnythingNorPersist()
  {
    var opened = false;

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input("  "), Upload(onOpen: () => opened = true)));

    Assert.False(opened);
    Assert.Empty(Storage.Paths);
    Assert.Equal(0, await CountDocumentsAsync());
  }

  [Fact]
  public async Task CreateAsync_FileInvalid_DoesNotOpenStreamNorStoreAnythingNorPersist()
  {
    var opened = false;

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(), Upload("notes.txt", onOpen: () => opened = true)));

    Assert.False(opened);
    Assert.Empty(Storage.Paths);
    Assert.Equal(0, await CountDocumentsAsync());
  }

  [Fact]
  public async Task ListAsync_ReturnsOwnDocumentsOrderedByNameCaseInsensitively()
  {
    await Service.CreateAsync(Input("beta"), Upload());
    await Service.CreateAsync(Input("Alpha"), Upload());
    await Service.CreateAsync(Input("Charlie"), Upload());

    var items = await Service.ListAsync();

    Assert.Equal(["Alpha", "beta", "Charlie"], items.Select(i => i.Name));
  }

  [Fact]
  public async Task ListAsync_ReturnsLanguageCodeTagsVersionCountAndLatestVersionDate()
  {
    var created = await Service.CreateAsync(Input("Main", SeedData.EnglishLanguageId, ["Unity", "Games"]), Upload());
    Clock.UtcNow = Now.AddDays(3);
    await Service.AddVersionAsync(created.Id, Upload("cv2.pdf"));

    var item = Assert.Single(await Service.ListAsync());

    Assert.Equal(created.Id, item.Id);
    Assert.Equal("en", item.LanguageCode);
    Assert.Equal(["Unity", "Games"], item.Tags);
    Assert.Equal(2, item.VersionCount);
    Assert.Equal(Now.AddDays(3), item.LatestVersionAt);
  }

  [Fact]
  public async Task ListAsync_RussianLanguage_ReturnsRuCode()
  {
    await Service.CreateAsync(Input(), Upload());

    Assert.Equal("ru", Assert.Single(await Service.ListAsync()).LanguageCode);
  }

  [Fact]
  public async Task ListAsync_DocumentOfAnotherUser_IsNotListed()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(other.Id).CreateAsync(Input("Foreign"), Upload());

    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public async Task FindAsync_Existing_ReturnsVersionsNewestFirst()
  {
    var created = await Service.CreateAsync(Input(), Upload("old.pdf"));
    Clock.UtcNow = Now.AddDays(1);
    await Service.AddVersionAsync(created.Id, Upload("new.pdf"));

    var details = await Service.FindAsync(created.Id);

    Assert.NotNull(details);
    Assert.Equal(["new.pdf", "old.pdf"], details.Versions.Select(v => v.FileName));
  }

  [Fact]
  public async Task FindAsync_Unknown_ReturnsNull()
  {
    Assert.Null(await Service.FindAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task FindAsync_DocumentOfAnotherUser_ReturnsNull()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input(), Upload());

    Assert.Null(await Service.FindAsync(foreign.Id));
  }

  [Fact]
  public async Task UpdateAsync_Valid_ChangesNameLanguageAndTags()
  {
    var created = await Service.CreateAsync(Input("Old"), Upload());

    var updated = await Service.UpdateAsync(created.Id, Input("  New ", SeedData.EnglishLanguageId, [" a ", "A", "b"]));

    Assert.Equal("New", updated.Name);
    Assert.Equal(SeedData.EnglishLanguageId, updated.LanguageId);
    Assert.Equal(["a", "b"], updated.Tags);
    var reloaded = await Service.FindAsync(created.Id);
    Assert.Equal("New", reloaded!.Name);
    Assert.Equal(["a", "b"], reloaded.Tags);
  }

  [Fact]
  public async Task UpdateAsync_Valid_DoesNotTouchVersionsOrFiles()
  {
    var created = await Service.CreateAsync(Input("Old"), Upload());
    var pathsBefore = Storage.Paths.ToList();

    var updated = await Service.UpdateAsync(created.Id, Input("New"));

    Assert.Equal(created.Versions.Select(v => v.Id), updated.Versions.Select(v => v.Id));
    Assert.Equal(pathsBefore, Storage.Paths);
  }

  [Fact]
  public async Task UpdateAsync_BlankName_ThrowsValidationKeyedByName()
  {
    var created = await Service.CreateAsync(Input("Old"), Upload());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, Input(" ")));

    Assert.Contains(nameof(CvInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_NullLanguage_ThrowsValidationKeyedByLanguageId()
  {
    var created = await Service.CreateAsync(Input("Old"), Upload());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, NoLanguageInput()));

    Assert.Contains(nameof(CvInput.LanguageId), exception.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_Unknown_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(Guid.NewGuid(), Input()));
  }
}
