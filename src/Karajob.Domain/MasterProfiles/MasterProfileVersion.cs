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
    // Not Require.NotBlank: trimming would rewrite the user's Markdown.
    ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    MasterProfileId = Require.NotEmpty(masterProfileId);
    Markdown = markdown;
    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    CreatedAt = UtcGuard.Ensure(createdAt);
    RestoredFromVersionId = restoredFromVersionId;
  }
}
