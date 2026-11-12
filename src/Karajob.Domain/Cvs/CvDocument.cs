using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public class CvDocument : IUserOwned
{
  private readonly List<CvVersion> versions = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public string Language { get; set => field = Require.NotBlank(value); }
  public DateTime CreatedAt { get; private set; }
  public IReadOnlyCollection<CvVersion> Versions => versions;

  public CvVersion AddVersion(
    string fileName,
    string storagePath,
    string contentType,
    long sizeBytes,
    string? comment,
    DateTime createdAt)
  {
    var version = new CvVersion(UserId, Id, fileName, storagePath, contentType, sizeBytes, comment, createdAt);
    versions.Add(version);
    return version;
  }

  public CvDocument(Guid userId, string name, string language, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = name;
    Language = language;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
