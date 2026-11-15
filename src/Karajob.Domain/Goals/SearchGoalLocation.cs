using Karajob.Domain.Common;

namespace Karajob.Domain.Goals;

public class SearchGoalLocation : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid SearchGoalId { get; private set; }
  public Guid LocationId { get; private set; }

  internal SearchGoalLocation(Guid userId, Guid searchGoalId, Guid locationId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    SearchGoalId = Require.NotEmpty(searchGoalId);
    LocationId = Require.NotEmpty(locationId);
  }
}
