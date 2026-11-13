using Karajob.Application.Persistence;
using Karajob.Application.Users;

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

  public Task DeleteAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IReadOnlyList<CvOption>> ListCvOptionsAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
