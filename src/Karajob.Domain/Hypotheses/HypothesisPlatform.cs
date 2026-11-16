using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class HypothesisPlatform : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public Guid PlatformId { get; private set; }

  internal HypothesisPlatform(Guid userId, Guid hypothesisId, Guid platformId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    PlatformId = Require.NotEmpty(platformId);
  }
}
