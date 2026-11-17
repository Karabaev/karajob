using Karajob.Domain.Common;

namespace Karajob.Domain.Tracks;

public class TrackPlatform : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid TrackId { get; private set; }
  public Guid PlatformId { get; private set; }

  internal TrackPlatform(Guid userId, Guid trackId, Guid platformId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    TrackId = Require.NotEmpty(trackId);
    PlatformId = Require.NotEmpty(platformId);
  }
}
