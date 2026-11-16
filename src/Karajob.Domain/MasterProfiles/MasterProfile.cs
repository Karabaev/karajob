using Karajob.Domain.Common;

namespace Karajob.Domain.MasterProfiles;

public class MasterProfile : IUserOwned
{
  private readonly List<MasterProfileVersion> versions = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public IReadOnlyCollection<MasterProfileVersion> Versions => versions;

  public MasterProfileVersion CurrentVersion => throw new NotImplementedException();

  public MasterProfileVersion AddVersion(
    string markdown,
    string? comment,
    DateTime createdAt,
    Guid? restoredFromVersionId = null)
  {
    throw new NotImplementedException();
  }

  public MasterProfile(Guid userId, string markdown, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    CreatedAt = UtcGuard.Ensure(createdAt);
  }

  // EF Core: the public constructor's markdown seeds the first version and has no column of its own.
  private MasterProfile()
  {
  }
}
