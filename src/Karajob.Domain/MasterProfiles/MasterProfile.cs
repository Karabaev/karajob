using Karajob.Domain.Common;

namespace Karajob.Domain.MasterProfiles;

public class MasterProfile : IUserOwned
{
  private readonly List<MasterProfileVersion> versions = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public IReadOnlyCollection<MasterProfileVersion> Versions => versions;

  public MasterProfileVersion CurrentVersion => NewestFirst()[0];

  // Ties on CreatedAt go to the version added later: v7 Ids are not ordered within one millisecond.
  public IReadOnlyList<MasterProfileVersion> NewestFirst()
  {
    return versions
      .Select((version, index) => (version, index))
      .OrderByDescending(x => x.version.CreatedAt)
      .ThenByDescending(x => x.index)
      .Select(x => x.version)
      .ToList();
  }

  public MasterProfileVersion AddVersion(
    string markdown,
    string? comment,
    DateTime createdAt,
    Guid? restoredFromVersionId = null)
  {
    if (restoredFromVersionId is { } sourceId && versions.All(v => v.Id != sourceId))
    {
      throw new KeyNotFoundException($"Master profile version {sourceId} was not found.");
    }

    var version = new MasterProfileVersion(UserId, Id, markdown, comment, createdAt, restoredFromVersionId);
    versions.Add(version);
    return version;
  }

  public MasterProfile(Guid userId, string markdown, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    CreatedAt = UtcGuard.Ensure(createdAt);
    AddVersion(markdown, null, createdAt);
  }

  // EF Core: the public constructor's markdown seeds the first version and has no column of its own.
  private MasterProfile()
  {
  }
}
