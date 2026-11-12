using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public class CvDocument : IUserOwned
{
  private readonly List<CvVersion> versions = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set; }
  public string Language { get; set; }
  public DateTime CreatedAt { get; private set; }
  public IReadOnlyCollection<CvVersion> Versions => versions;

  public CvVersion AddVersion(
    string fileName,
    string storagePath,
    string contentType,
    long sizeBytes,
    string? comment,
    DateTime createdAt) => throw new NotImplementedException();

  public CvDocument(Guid userId, string name, string language, DateTime createdAt) =>
    throw new NotImplementedException();
}
