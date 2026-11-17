using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public class CvDocumentTag : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid CvDocumentId { get; private set; }
  public Guid TagId { get; private set; }

  internal CvDocumentTag(Guid userId, Guid cvDocumentId, Guid tagId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    CvDocumentId = Require.NotEmpty(cvDocumentId);
    TagId = Require.NotEmpty(tagId);
  }
}
