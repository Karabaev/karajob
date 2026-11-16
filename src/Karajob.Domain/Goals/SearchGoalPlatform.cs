using Karajob.Domain.Common;

namespace Karajob.Domain.Goals;

public class SearchGoalPlatform : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid SearchGoalId { get; private set; }
  public Guid PlatformId { get; private set; }

  internal SearchGoalPlatform(Guid userId, Guid searchGoalId, Guid platformId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    SearchGoalId = Require.NotEmpty(searchGoalId);
    PlatformId = Require.NotEmpty(platformId);
  }
}
