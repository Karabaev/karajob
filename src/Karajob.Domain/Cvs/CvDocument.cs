using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public class CvDocument : IUserOwned
{
  private readonly List<CvVersion> versions = [];
  private readonly List<CvDocumentTag> tags = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public Guid LanguageId { get; set => field = Require.NotEmpty(value); }
  public IReadOnlyCollection<CvDocumentTag> Tags => tags;
  public DateTime CreatedAt { get; private set; }
  public IReadOnlyCollection<CvVersion> Versions => versions;

  public void SetTags(IEnumerable<Guid> tagIds)
  {
    var wanted = tagIds.Select(id => Require.NotEmpty(id, nameof(tagIds))).Distinct().ToList();
    tags.RemoveAll(t => !wanted.Contains(t.TagId));
    foreach (var tagId in wanted.Where(id => tags.All(t => t.TagId != id)))
    {
      tags.Add(new CvDocumentTag(UserId, Id, tagId));
    }
  }

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

  public void RemoveVersion(Guid versionId)
  {
    var version = versions.FirstOrDefault(v => v.Id == versionId)
      ?? throw new KeyNotFoundException($"CV version {versionId} was not found.");
    if (versions.Count == 1)
    {
      throw new InvalidOperationException("A CV must keep at least one version.");
    }

    versions.Remove(version);
  }

  public CvDocument(Guid userId, string name, Guid languageId, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = name;
    LanguageId = languageId;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
