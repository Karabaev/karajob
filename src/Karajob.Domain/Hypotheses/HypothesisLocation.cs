using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class HypothesisLocation : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public Guid LocationId { get; private set; }

  internal HypothesisLocation(Guid userId, Guid hypothesisId, Guid locationId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    LocationId = Require.NotEmpty(locationId);
  }
}
