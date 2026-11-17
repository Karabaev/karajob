using Karajob.Domain.Common;

namespace Karajob.Domain.Tracks;

public class TrackEngine : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid TrackId { get; private set; }
  public Guid EngineId { get; private set; }

  internal TrackEngine(Guid userId, Guid trackId, Guid engineId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    TrackId = Require.NotEmpty(trackId);
    EngineId = Require.NotEmpty(engineId);
  }
}
