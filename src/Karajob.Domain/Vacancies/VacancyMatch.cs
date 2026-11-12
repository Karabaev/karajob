using Karajob.Domain.Common;

namespace Karajob.Domain.Vacancies;

public class VacancyMatch : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid VacancyId { get; private set; }
  public Guid HypothesisId { get; private set; }
  public int? Score { get; private set; }
  public string? Rationale { get; private set; }
  public MatchAuthor MatchedBy { get; private set; }

  internal void Update(int? score, string? rationale, MatchAuthor matchedBy)
  {
    if (score is not null)
    {
      ArgumentOutOfRangeException.ThrowIfLessThan(score.Value, 0, nameof(score));
      ArgumentOutOfRangeException.ThrowIfGreaterThan(score.Value, 100, nameof(score));
    }

    Score = score;
    Rationale = rationale;
    MatchedBy = matchedBy;
  }

  internal VacancyMatch(
    Guid userId,
    Guid vacancyId,
    Guid hypothesisId,
    int? score,
    string? rationale,
    MatchAuthor matchedBy)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    VacancyId = Require.NotEmpty(vacancyId);
    HypothesisId = Require.NotEmpty(hypothesisId);
    Update(score, rationale, matchedBy);
  }
}
