using Karajob.Application.Persistence;

namespace Karajob.Application.Applications;

public class ApplicationService(IKarajobDbContextFactory dbContextFactory)
{
  public Task<IReadOnlyList<ApplicationHistoryItem>> ListHistoryAsync(
    ApplicationHistoryFilter filter,
    CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
