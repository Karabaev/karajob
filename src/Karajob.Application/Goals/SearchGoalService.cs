using Karajob.Application.Persistence;
using Karajob.Application.Users;

namespace Karajob.Application.Goals;

public class SearchGoalService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser)
{
  public Task<SearchGoalDetails> GetAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<SearchGoalDetails> UpdateAsync(SearchGoalInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
