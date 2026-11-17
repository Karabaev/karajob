using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Tracks;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Vacancies;

public class Vacancy : IUserOwned
{
  public const int TitleMaxLength = 300;

  private readonly List<VacancyMatch> matches = [];
  private readonly List<VacancyPlatform> platforms = [];
  private readonly List<VacancyEngine> engines = [];
  private readonly List<ApplicationEvent> events = [];
  private DateTime? publishedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid? CompanyId { get; set; }
  public Guid? RoleId { get; set; }
  public string Url { get; private set; }
  public string? Title { get; set; }
  public Guid? LocationId { get; set; }
  public IReadOnlyCollection<VacancyPlatform> Platforms => platforms;
  public IReadOnlyCollection<VacancyEngine> Engines => engines;
  public Guid? LanguageId { get; set; }
  public WorkFormat? WorkFormat { get; set; }
  public SalaryRange? Salary { get; set; }
  public string? Description { get; set; }
  public string? Notes { get; set; }
  public VacancySource Source { get; private set; }
  public DateTime DiscoveredAt { get; private set; }
  public VacancyStage Stage { get; private set; }
  public DismissReason? DismissReason { get; private set; }
  public IReadOnlyCollection<VacancyMatch> Matches => matches;
  public IReadOnlyCollection<ApplicationEvent> Events => events;
  public JobApplication? Application { get; private set; }

  public VacancyStage? ClosedFromStage => Stage == VacancyStage.Closed ? LatestClosedEvent()?.FromStage : null;

  public DateTime? ClosedAt => Stage == VacancyStage.Closed ? LatestClosedEvent()?.OccurredAt : null;

  public VacancyStage? ReopenTarget => Stage == VacancyStage.Closed ? ClosedFromStage ?? VacancyStage.Applied : null;

  public DateTime? PublishedAt
  {
    get => publishedAt;
    set => publishedAt = UtcGuard.Ensure(value);
  }

  public void ChangeUrl(string url)
  {
    Url = Normalize.VacancyUrl(url);
  }

  public void SetPlatforms(IEnumerable<Guid> platformIds)
  {
    var wanted = platformIds.Select(id => Require.NotEmpty(id, nameof(platformIds))).Distinct().ToList();
    platforms.RemoveAll(p => !wanted.Contains(p.PlatformId));
    foreach (var platformId in wanted.Where(id => platforms.All(p => p.PlatformId != id)))
    {
      platforms.Add(new VacancyPlatform(UserId, Id, platformId));
    }
  }

  public void SetEngines(IEnumerable<Guid> engineIds)
  {
    var wanted = engineIds.Select(id => Require.NotEmpty(id, nameof(engineIds))).Distinct().ToList();
    engines.RemoveAll(e => !wanted.Contains(e.EngineId));
    foreach (var engineId in wanted.Where(id => engines.All(e => e.EngineId != id)))
    {
      engines.Add(new VacancyEngine(UserId, Id, engineId));
    }
  }

  public VacancyMatch SetMatch(Guid trackId, int? score, string? rationale, MatchAuthor matchedBy)
  {
    Require.NotEmpty(trackId);
    var existing = matches.FirstOrDefault(m => m.TrackId == trackId);
    if (existing is not null)
    {
      existing.Update(score, rationale, matchedBy);
      return existing;
    }

    var match = new VacancyMatch(UserId, Id, trackId, score, rationale, matchedBy);
    matches.Add(match);
    return match;
  }

  public void RemoveMatch(Guid trackId)
  {
    if (matches.All(m => m.TrackId != trackId)) return;
    if (trackId == Application?.TrackId)
    {
      throw new InvalidOperationException("The track of the application cannot be unmatched.");
    }

    if (matches.Count == 1 && Stage is not (VacancyStage.Inbox or VacancyStage.Dismissed))
    {
      throw new InvalidOperationException($"A vacancy in {Stage} requires at least one matched track.");
    }

    matches.RemoveAll(m => m.TrackId == trackId);
  }

  public ApplicationEvent AddEvent(ApplicationEventType type, DateTime at, string? note, TimeZoneInfo zone)
  {
    UtcGuard.Ensure(at);
    if (type is ApplicationEventType.StageChanged or ApplicationEventType.Rejected or ApplicationEventType.Offer)
    {
      throw new ArgumentException($"{type} events cannot be added manually.", nameof(type));
    }

    if (UserTime.LocalDate(at, zone) < UserTime.LocalDate(DiscoveredAt, zone))
    {
      throw new ArgumentException("The event cannot be earlier than the day the vacancy was discovered.", nameof(at));
    }

    if (type != ApplicationEventType.Note && Application is null)
    {
      throw new InvalidOperationException($"{type} requires an application.");
    }

    var added = new ApplicationEvent(UserId, Id, type, at, null, null, note);
    events.Add(added);
    return added;
  }

  public void RemoveEvent(Guid eventId)
  {
    var existing = events.FirstOrDefault(e => e.Id == eventId)
      ?? throw new KeyNotFoundException($"Event {eventId} was not found.");
    if (existing.Type == ApplicationEventType.StageChanged)
    {
      throw new InvalidOperationException("Stage transitions are immutable.");
    }

    events.Remove(existing);
  }

  public ApplicationEvent MoveTo(VacancyStage target, DateTime at)
  {
    UtcGuard.Ensure(at);
    var reopensApplication = Stage == VacancyStage.Closed && target == VacancyStage.Applied;
    if (target is VacancyStage.Closed or VacancyStage.Dismissed || target == VacancyStage.Applied && !reopensApplication)
    {
      throw new StageTransitionException(Stage, target, $"Use the dedicated method to move to {target}.");
    }

    EnsureCanMove(target);
    var entersBoard = Stage is VacancyStage.Inbox or VacancyStage.Dismissed && target != VacancyStage.Inbox;
    if (entersBoard && matches.Count == 0)
    {
      throw new StageTransitionException(Stage, target, $"{target} requires at least one matched track.");
    }

    if (Stage == VacancyStage.Dismissed)
    {
      DismissReason = null;
    }

    if (Stage == VacancyStage.Closed)
    {
      Application!.SetOutcome(null);
    }

    return ChangeStage(target, at, null);
  }

  public ApplicationEvent Dismiss(DismissReason reason, string? comment, DateTime at)
  {
    UtcGuard.Ensure(at);
    EnsureCanMove(VacancyStage.Dismissed);
    DismissReason = reason;
    return ChangeStage(VacancyStage.Dismissed, at, comment);
  }

  public ApplicationEvent Apply(
    Guid trackId,
    Guid cvVersionId,
    ApplicationChannel channel,
    DateTime appliedAt,
    string? coverLetter)
  {
    Require.NotEmpty(trackId);
    Require.NotEmpty(cvVersionId);
    UtcGuard.Ensure(appliedAt);
    EnsureCanMove(VacancyStage.Applied);
    if (Application is not null)
    {
      throw new StageTransitionException(Stage, VacancyStage.Applied, "The vacancy already has an application; reopen it instead.");
    }

    if (matches.All(m => m.TrackId != trackId))
    {
      throw new StageTransitionException(Stage, VacancyStage.Applied, "The track is not matched to this vacancy.");
    }

    Application = new JobApplication(UserId, Id, trackId, cvVersionId, channel, appliedAt, coverLetter);
    return ChangeStage(VacancyStage.Applied, appliedAt, null);
  }

  public ApplicationEvent Close(ApplicationOutcome outcome, DateTime at)
  {
    UtcGuard.Ensure(at);
    EnsureCanMove(VacancyStage.Closed);
    Application!.SetOutcome(outcome);
    return ChangeStage(VacancyStage.Closed, at, null);
  }

  public ApplicationEvent Reopen(VacancyStage target, DateTime at)
  {
    if (Stage != VacancyStage.Closed || target is not (VacancyStage.Applied or VacancyStage.Interviewing))
    {
      throw new StageTransitionException(Stage, target, $"Cannot reopen a vacancy from {Stage} to {target}.");
    }

    return MoveTo(target, at);
  }

  private ApplicationEvent? LatestClosedEvent()
  {
    return events
      .Where(e => e.Type == ApplicationEventType.StageChanged && e.ToStage == VacancyStage.Closed)
      .OrderBy(e => e.OccurredAt)
      .ThenBy(e => e.Id)
      .LastOrDefault();
  }

  private void EnsureCanMove(VacancyStage target)
  {
    if (!VacancyStageRules.CanMove(Stage, target))
    {
      throw new StageTransitionException(Stage, target, $"Cannot move a vacancy from {Stage} to {target}.");
    }
  }

  private ApplicationEvent ChangeStage(VacancyStage target, DateTime at, string? note)
  {
    var stageChanged = new ApplicationEvent(UserId, Id, ApplicationEventType.StageChanged, at, Stage, target, note);
    events.Add(stageChanged);
    Stage = target;
    return stageChanged;
  }

  public Vacancy(Guid userId, string url, VacancySource source, DateTime discoveredAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Url = Normalize.VacancyUrl(url);
    Source = source;
    DiscoveredAt = UtcGuard.Ensure(discoveredAt);
    Stage = VacancyStage.Inbox;
  }
}
