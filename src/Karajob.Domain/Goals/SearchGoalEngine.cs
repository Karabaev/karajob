using Karajob.Domain.Common;

namespace Karajob.Domain.Goals;

public class SearchGoalEngine : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid SearchGoalId { get; private set; }
  public Guid EngineId { get; private set; }

  internal SearchGoalEngine(Guid userId, Guid searchGoalId, Guid engineId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    SearchGoalId = Require.NotEmpty(searchGoalId);
    EngineId = Require.NotEmpty(engineId);
  }
}
