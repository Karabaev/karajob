using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class Hypothesis : IUserOwned
{
  private readonly List<HypothesisLocation> locations = [];
  private DateTime? startedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public Guid RoleId { get; set => field = Require.NotEmpty(value); }
  public IReadOnlyCollection<HypothesisLocation> Locations => locations;
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

  public void SetLocations(IEnumerable<Guid> locationIds)
  {
    var wanted = locationIds.Select(id => Require.NotEmpty(id, nameof(locationIds))).ToHashSet();
    locations.RemoveAll(l => !wanted.Contains(l.LocationId));
    foreach (var locationId in wanted.Where(id => locations.All(l => l.LocationId != id)))
    {
      locations.Add(new HypothesisLocation(UserId, Id, locationId));
    }
  }

  public void ChangeStatus(HypothesisStatus status, DateTime utcNow)
  {
    Status = status;
    if (status is HypothesisStatus.Active or HypothesisStatus.Expanded && StartedAt is null)
    {
      StartedAt = utcNow;
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
