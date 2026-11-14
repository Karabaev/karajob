using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Vacancies;

public class Vacancy : IUserOwned
{
  private readonly List<VacancyMatch> matches = [];
  private readonly List<ApplicationEvent> events = [];
  private DateTime? publishedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid? CompanyId { get; set; }
  public Guid? RoleId { get; set; }
  public string Url { get; private set; }
  public Guid? LocationId { get; set; }
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

  public DateTime? PublishedAt
  {
    get => publishedAt;
    set => publishedAt = UtcGuard.Ensure(value);
  }

  public void ChangeUrl(string url)
  {
    Url = Normalize.VacancyUrl(url);
  }

  public VacancyMatch SetMatch(Guid hypothesisId, int? score, string? rationale, MatchAuthor matchedBy)
  {
    Require.NotEmpty(hypothesisId);
    var existing = matches.FirstOrDefault(m => m.HypothesisId == hypothesisId);
    if (existing is not null)
    {
      existing.Update(score, rationale, matchedBy);
      return existing;
    }

    var match = new VacancyMatch(UserId, Id, hypothesisId, score, rationale, matchedBy);
    matches.Add(match);
    return match;
  }

  public void RemoveMatch(Guid hypothesisId)
  {
    if (matches.All(m => m.HypothesisId != hypothesisId)) return;
    if (hypothesisId == Application?.HypothesisId)
    {
      throw new InvalidOperationException("The hypothesis of the application cannot be unmatched.");
    }

    if (matches.Count == 1 && Stage is not (VacancyStage.Inbox or VacancyStage.Dismissed))
    {
      throw new InvalidOperationException($"A vacancy in {Stage} requires at least one matched hypothesis.");
    }

    matches.RemoveAll(m => m.HypothesisId == hypothesisId);
  }

  public ApplicationEvent AddEvent(ApplicationEventType type, DateTime at, string? note)
  {
    UtcGuard.Ensure(at);
    if (type is ApplicationEventType.StageChanged or ApplicationEventType.Rejected or ApplicationEventType.Offer)
    {
      throw new ArgumentException($"{type} events cannot be added manually.", nameof(type));
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
      throw new StageTransitionException(Stage, target, $"{target} requires at least one matched hypothesis.");
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
    Guid hypothesisId,
    Guid cvVersionId,
    ApplicationChannel channel,
    DateTime appliedAt,
    string? coverLetter)
  {
    Require.NotEmpty(hypothesisId);
    Require.NotEmpty(cvVersionId);
    UtcGuard.Ensure(appliedAt);
    EnsureCanMove(VacancyStage.Applied);
    if (Application is not null)
    {
      throw new StageTransitionException(Stage, VacancyStage.Applied, "The vacancy already has an application; reopen it instead.");
    }

    if (matches.All(m => m.HypothesisId != hypothesisId))
    {
      throw new StageTransitionException(Stage, VacancyStage.Applied, "The hypothesis is not matched to this vacancy.");
    }

    Application = new JobApplication(UserId, Id, hypothesisId, cvVersionId, channel, appliedAt, coverLetter);
    return ChangeStage(VacancyStage.Applied, appliedAt, null);
  }

  public ApplicationEvent Close(ApplicationOutcome outcome, DateTime at)
  {
    UtcGuard.Ensure(at);
    EnsureCanMove(VacancyStage.Closed);
    Application!.SetOutcome(outcome);
    return ChangeStage(VacancyStage.Closed, at, null);
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
