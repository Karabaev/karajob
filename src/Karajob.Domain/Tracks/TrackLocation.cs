using Karajob.Domain.Common;

namespace Karajob.Domain.Tracks;

public class TrackLocation : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid TrackId { get; private set; }
  public Guid LocationId { get; private set; }

  internal TrackLocation(Guid userId, Guid trackId, Guid locationId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    TrackId = Require.NotEmpty(trackId);
    LocationId = Require.NotEmpty(locationId);
  }
}
