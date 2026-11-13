using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Hypotheses;

public class HypothesisService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, TimeProvider timeProvider)
{
  public Task<IReadOnlyList<HypothesisListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<HypothesisDetails?> FindAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<HypothesisDetails> CreateAsync(HypothesisInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<HypothesisDetails> UpdateAsync(Guid hypothesisId, HypothesisInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public async Task DeleteAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.FirstOrDefaultAsync(h => h.Id == hypothesisId, cancellationToken)
      ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");

    var used = await db.VacancyMatches.AnyAsync(m => m.HypothesisId == hypothesisId, cancellationToken)
      || await db.JobApplications.AnyAsync(a => a.HypothesisId == hypothesisId, cancellationToken);
    if (used)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = "Гипотеза используется в вакансиях или откликах — переведите её в статус Disabled",
      });
    }

    db.Hypotheses.Remove(hypothesis);
    await db.SaveChangesAsync(cancellationToken);
  }

  public Task<IReadOnlyList<CvOption>> ListCvOptionsAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
