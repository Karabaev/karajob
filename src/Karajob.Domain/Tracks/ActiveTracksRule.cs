namespace Karajob.Domain.Tracks;

public static class ActiveTracksRule
{
  public const int MinActive = 2;
  public const int MaxActive = 3;

  public static bool IsActive(TrackStatus status)
  {
    return status is TrackStatus.Active or TrackStatus.Expanded;
  }

  public static ActiveTracksStatus Check(int activeCount)
  {
    if (activeCount < MinActive) return ActiveTracksStatus.TooFew;
    if (activeCount > MaxActive) return ActiveTracksStatus.TooMany;

    return ActiveTracksStatus.Ok;
  }
}
