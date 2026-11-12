using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public class CvVersion : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid CvDocumentId { get; private set; }
  public string FileName { get; private set; }
  public string StoragePath { get; private set; }
  public string ContentType { get; private set; }
  public long SizeBytes { get; private set; }
  public string? Comment { get; set; }
  public DateTime CreatedAt { get; private set; }

  internal CvVersion(
    Guid userId,
    Guid cvDocumentId,
    string fileName,
    string storagePath,
    string contentType,
    long sizeBytes,
    string? comment,
    DateTime createdAt)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    CvDocumentId = Require.NotEmpty(cvDocumentId);
    FileName = Require.NotBlank(fileName);
    StoragePath = Require.NotBlank(storagePath);
    ContentType = Require.NotBlank(contentType);
    SizeBytes = sizeBytes;
    Comment = comment;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
