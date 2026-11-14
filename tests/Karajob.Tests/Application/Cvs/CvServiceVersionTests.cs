using Karajob.Application.Common;
using Karajob.Application.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Cvs;

public class CvServiceVersionTests : CvServiceFixture
{
  [Fact]
  public async Task AddVersionAsync_Valid_ReturnsTwoVersionsNewestFirst()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));
    Clock.UtcNow = Now.AddDays(1);

    var details = await Service.AddVersionAsync(created.Id, Upload("v2.pdf", comment: " second "));

    Assert.Equal(2, details.Versions.Count);
    Assert.Equal("v2.pdf", details.Versions[0].FileName);
    Assert.Equal("second", details.Versions[0].Comment);
    Assert.Equal(Now.AddDays(1), details.Versions[0].CreatedAt);
    Assert.Equal("v1.pdf", details.Versions[1].FileName);
  }

  [Fact]
  public async Task AddVersionAsync_Valid_StoresSecondFileUnderDifferentPath()
  {
    var created = await Service.CreateAsync(Input(), Upload("v1.pdf"));

    await Service.AddVersionAsync(created.Id, Upload("v2.docx", [9, 9, 9]));

    Assert.Equal(2, Storage.Paths.Distinct().Count());
    Assert.Contains(
      Storage.Paths,
      p => p.StartsWith($"cv/{SeedData.OwnerId:N}/", StringComparison.Ordinal) && p.EndsWith(".docx", StringComparison.Ordinal));
  }

  [Theory]
  [InlineData("notes.txt", 10)]
  [InlineData("cv.pdf", 0)]
  [InlineData("cv.pdf", 5L * 1024 * 1024 + 1)]
  public async Task AddVersionAsync_InvalidFile_ThrowsValidationKeyedByFileAndStoresNothingNew(string fileName, long size)
  {
    var created = await Service.CreateAsync(Input(), Upload());
    var opened = false;

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.AddVersionAsync(created.Id, Upload(fileName, sizeBytes: size, onOpen: () => opened = true)));

    Assert.Contains(CvService.FileErrorKey, exception.Errors.Keys);
    Assert.False(opened);
    Assert.Single(Storage.Paths);
    Assert.Equal(1, await CountVersionsAsync());
  }

  [Fact]
  public async Task AddVersionAsync_Unknown_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.AddVersionAsync(Guid.NewGuid(), Upload()));
  }

  [Fact]
  public async Task AddVersionAsync_DocumentOfAnotherUser_ThrowsKeyNotFoundAndStoresNothing()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input(), Upload());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.AddVersionAsync(foreign.Id, Upload()));

    Assert.Single(Storage.Paths);
  }

  [Fact]
  public async Task OpenVersionAsync_Existing_ReturnsStoredBytesNameAndContentType()
  {
    var created = await Service.CreateAsync(Input(), Upload("My CV.pdf", PdfBytes));

    var file = await Service.OpenVersionAsync(created.Versions[0].Id);

    Assert.NotNull(file);
    await using var content = file.Content;
    using var buffer = new MemoryStream();
    await content.CopyToAsync(buffer);
    Assert.Equal(PdfBytes, buffer.ToArray());
    Assert.Equal("My CV.pdf", file.FileName);
    Assert.Equal("application/pdf", file.ContentType);
  }

  [Fact]
  public async Task OpenVersionAsync_Docx_ReturnsDocxContentType()
  {
    var created = await Service.CreateAsync(Input(), Upload("cv.docx"));

    var file = await Service.OpenVersionAsync(created.Versions[0].Id);

    Assert.NotNull(file);
    await file.Content.DisposeAsync();
    Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", file.ContentType);
  }

  [Fact]
  public async Task OpenVersionAsync_Unknown_ReturnsNull()
  {
    Assert.Null(await Service.OpenVersionAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task OpenVersionAsync_VersionOfAnotherUser_ReturnsNull()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input(), Upload());

    Assert.Null(await Service.OpenVersionAsync(foreign.Versions[0].Id));
  }
}
