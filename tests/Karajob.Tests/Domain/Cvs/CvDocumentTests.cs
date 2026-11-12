using Karajob.Domain.Cvs;

namespace Karajob.Tests.Domain.Cvs;

public class CvDocumentTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static CvDocument Create() => new(Guid.NewGuid(), "Main CV", "en", Now);

  [Fact]
  public void Ctor_Valid_SetsFields()
  {
    var userId = Guid.NewGuid();

    var doc = new CvDocument(userId, "Main CV", "en", Now);

    Assert.NotEqual(Guid.Empty, doc.Id);
    Assert.Equal(userId, doc.UserId);
    Assert.Equal("Main CV", doc.Name);
    Assert.Equal("en", doc.Language);
    Assert.Equal(Now, doc.CreatedAt);
    Assert.Empty(doc.Versions);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new CvDocument(Guid.Empty, "Main CV", "en", Now));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => new CvDocument(Guid.NewGuid(), name, "en", Now));
  }

  [Fact]
  public void AddVersion_Valid_AddsVersionLinkedToDocument()
  {
    var doc = Create();

    var version = doc.AddVersion("cv.pdf", "files/cv/1.pdf", "application/pdf", 1234, "first", Now);

    Assert.Same(version, Assert.Single(doc.Versions));
    Assert.NotEqual(Guid.Empty, version.Id);
    Assert.Equal(doc.UserId, version.UserId);
    Assert.Equal(doc.Id, version.CvDocumentId);
    Assert.Equal("cv.pdf", version.FileName);
    Assert.Equal("files/cv/1.pdf", version.StoragePath);
    Assert.Equal("application/pdf", version.ContentType);
    Assert.Equal(1234, version.SizeBytes);
    Assert.Equal("first", version.Comment);
    Assert.Equal(Now, version.CreatedAt);
  }

  [Theory]
  [InlineData("", "p", "c")]
  [InlineData("   ", "p", "c")]
  [InlineData("f", "", "c")]
  [InlineData("f", "   ", "c")]
  [InlineData("f", "p", "")]
  [InlineData("f", "p", "   ")]
  public void AddVersion_BlankTextField_Throws(string fileName, string storagePath, string contentType)
  {
    var doc = Create();

    Assert.ThrowsAny<ArgumentException>(() => doc.AddVersion(fileName, storagePath, contentType, 10, null, Now));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void AddVersion_NonPositiveSize_Throws(long size)
  {
    var doc = Create();

    Assert.ThrowsAny<ArgumentException>(() => doc.AddVersion("f", "p", "c", size, null, Now));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void AddVersion_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    var doc = Create();

    Assert.ThrowsAny<ArgumentException>(() =>
      doc.AddVersion("f", "p", "c", 10, null, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }
}
