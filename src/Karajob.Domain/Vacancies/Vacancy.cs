using Karajob.Domain.Common;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Vacancies;

public class Vacancy : IUserOwned
{
  private readonly List<VacancyMatch> matches = [];
  private DateTime? publishedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid? CompanyId { get; set; }
  public string? Role { get; set; }
  public string Url { get; private set; }
  public string? Location { get; set; }
  public WorkFormat? WorkFormat { get; set; }
  public SalaryRange? Salary { get; set; }
  public string? Description { get; set; }
  public string? Notes { get; set; }
  public VacancySource Source { get; private set; }
  public DateTime DiscoveredAt { get; private set; }
  public VacancyStage Stage { get; private set; }
  public DismissReason? DismissReason { get; private set; }
  public IReadOnlyCollection<VacancyMatch> Matches => matches;

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
    matches.RemoveAll(m => m.HypothesisId == hypothesisId);
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
