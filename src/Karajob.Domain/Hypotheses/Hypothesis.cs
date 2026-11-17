using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class Hypothesis : IUserOwned
{
  private readonly List<HypothesisLocation> locations = [];
  private readonly List<HypothesisPlatform> platforms = [];
  private readonly List<HypothesisEngine> engines = [];
  private DateTime? startedAt;
  private DateTime? periodStartedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public Guid RoleId { get; set => field = Require.NotEmpty(value); }
  public IReadOnlyCollection<HypothesisLocation> Locations => locations;
  public IReadOnlyCollection<HypothesisPlatform> Platforms => platforms;
  public IReadOnlyCollection<HypothesisEngine> Engines => engines;
  public List<string> Industries { get; private set; } = [];
  public WorkFormat WorkFormat { get; set; }
  public Guid LanguageId { get; set => field = Require.NotEmpty(value); }
  public string? Positioning { get; set; }
  public Guid? PrimaryCvDocumentId { get; set; }
  public HypothesisStatus Status { get; set; }
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
      locations.Add(new HypothesisLocation(UserId, Id, locationId));
    }
  }

  public void SetPlatforms(IEnumerable<Guid> platformIds)
  {
    var wanted = platformIds.Select(id => Require.NotEmpty(id, nameof(platformIds))).Distinct().ToList();
    platforms.RemoveAll(p => !wanted.Contains(p.PlatformId));
    foreach (var platformId in wanted.Where(id => platforms.All(p => p.PlatformId != id)))
    {
      platforms.Add(new HypothesisPlatform(UserId, Id, platformId));
    }
  }

  public void SetEngines(IEnumerable<Guid> engineIds)
  {
    var wanted = engineIds.Select(id => Require.NotEmpty(id, nameof(engineIds))).Distinct().ToList();
    engines.RemoveAll(e => !wanted.Contains(e.EngineId));
    foreach (var engineId in wanted.Where(id => engines.All(e => e.EngineId != id)))
    {
      engines.Add(new HypothesisEngine(UserId, Id, engineId));
    }
  }

  public void ChangeStatus(HypothesisStatus status, DateTime utcNow)
  {
    Status = status;
    if (status is HypothesisStatus.Active or HypothesisStatus.Expanded && StartedAt is null)
    {
      StartedAt = utcNow;
      PeriodStartedAt = utcNow;
    }
  }

  public void ApplyDecision(HypothesisDecisionKind kind, DateTime utcNow)
  {
    if (!ActiveHypothesesRule.IsActive(Status)) throw new InvalidOperationException($"A decision cannot be applied to a {Status} hypothesis.");
    UtcGuard.Ensure(utcNow);

    switch (kind)
    {
      case HypothesisDecisionKind.Expand:
        Status = HypothesisStatus.Expanded;
        PeriodStartedAt = utcNow;
        break;
      case HypothesisDecisionKind.Disable:
        Status = HypothesisStatus.Disabled;
        break;
    }
  }

  public Hypothesis(Guid userId, string name, Guid roleId, Guid languageId, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = name;
    RoleId = roleId;
    LanguageId = languageId;
    Status = HypothesisStatus.Draft;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
