using Karajob.Domain.Common;

namespace Karajob.Domain.Tracks;

public class Track : IUserOwned
{
  private readonly List<TrackLocation> locations = [];
  private readonly List<TrackPlatform> platforms = [];
  private readonly List<TrackEngine> engines = [];
  private DateTime? startedAt;
  private DateTime? periodStartedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public Guid RoleId { get; set => field = Require.NotEmpty(value); }
  public IReadOnlyCollection<TrackLocation> Locations => locations;
  public IReadOnlyCollection<TrackPlatform> Platforms => platforms;
  public IReadOnlyCollection<TrackEngine> Engines => engines;
  public List<string> Industries { get; private set; } = [];
  public WorkFormat WorkFormat { get; set; }
  public Guid LanguageId { get; set => field = Require.NotEmpty(value); }
  public string? Positioning { get; set; }
  public Guid? PrimaryCvDocumentId { get; set; }
  public TrackStatus Status { get; set; }
  public DateTime CreatedAt { get; private set; }

  public DateTime? StartedAt
  {
    get => startedAt;
    set => startedAt = UtcGuard.Ensure(value);
  }

  public DateTime? PeriodStartedAt
  {
    get => periodStartedAt;
    private set => periodStartedAt = UtcGuard.Ensure(value);
  }

  public void SetLocations(IEnumerable<Guid> locationIds)
  {
    var wanted = locationIds.Select(id => Require.NotEmpty(id, nameof(locationIds))).Distinct().ToList();
    locations.RemoveAll(l => !wanted.Contains(l.LocationId));
    foreach (var locationId in wanted.Where(id => locations.All(l => l.LocationId != id)))
    {
      locations.Add(new TrackLocation(UserId, Id, locationId));
    }
  }

  public void SetPlatforms(IEnumerable<Guid> platformIds)
  {
    var wanted = platformIds.Select(id => Require.NotEmpty(id, nameof(platformIds))).Distinct().ToList();
    platforms.RemoveAll(p => !wanted.Contains(p.PlatformId));
    foreach (var platformId in wanted.Where(id => platforms.All(p => p.PlatformId != id)))
    {
      platforms.Add(new TrackPlatform(UserId, Id, platformId));
    }
  }

  public void SetEngines(IEnumerable<Guid> engineIds)
  {
    var wanted = engineIds.Select(id => Require.NotEmpty(id, nameof(engineIds))).Distinct().ToList();
    engines.RemoveAll(e => !wanted.Contains(e.EngineId));
    foreach (var engineId in wanted.Where(id => engines.All(e => e.EngineId != id)))
    {
      engines.Add(new TrackEngine(UserId, Id, engineId));
    }
  }

  public void ChangeStatus(TrackStatus status, DateTime utcNow)
  {
    Status = status;
    if (status is TrackStatus.Active or TrackStatus.Expanded && StartedAt is null)
    {
      StartedAt = utcNow;
      PeriodStartedAt = utcNow;
    }
  }

  public void ApplyDecision(TrackDecisionKind kind, DateTime utcNow)
  {
    if (!ActiveTracksRule.IsActive(Status)) throw new InvalidOperationException($"A decision cannot be applied to a {Status} track.");
    UtcGuard.Ensure(utcNow);

    switch (kind)
    {
      case TrackDecisionKind.Expand:
        Status = TrackStatus.Expanded;
        PeriodStartedAt = utcNow;
        break;
      case TrackDecisionKind.Disable:
        Status = TrackStatus.Disabled;
        break;
      case TrackDecisionKind.Continue:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }
  }

  public Track(Guid userId, string name, Guid roleId, Guid languageId, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = name;
    RoleId = roleId;
    LanguageId = languageId;
    Status = TrackStatus.Draft;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
