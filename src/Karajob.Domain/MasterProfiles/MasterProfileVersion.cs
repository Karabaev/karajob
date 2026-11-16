using Karajob.Domain.Common;

namespace Karajob.Domain.MasterProfiles;

public class MasterProfileVersion : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid MasterProfileId { get; private set; }
  public string Markdown { get; private set; }
  public string? Comment { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public Guid? RestoredFromVersionId { get; private set; }

  internal MasterProfileVersion(
    Guid userId,
    Guid masterProfileId,
    string markdown,
    string? comment,
    DateTime createdAt,
    Guid? restoredFromVersionId)
  {
    throw new NotImplementedException();
  }
}
