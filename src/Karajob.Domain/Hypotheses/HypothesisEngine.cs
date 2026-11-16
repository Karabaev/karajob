using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class HypothesisEngine : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public Guid EngineId { get; private set; }

  internal HypothesisEngine(Guid userId, Guid hypothesisId, Guid engineId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    EngineId = Require.NotEmpty(engineId);
  }
}
