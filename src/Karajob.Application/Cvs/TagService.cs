using Karajob.Application.Persistence;
using Karajob.Application.Users;

namespace Karajob.Application.Cvs;

public class TagService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser)
{
  public Task<IReadOnlyList<TagListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IReadOnlyList<TagItem>> SearchAsync(string? prefix, int limit, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task SetColorAsync(Guid tagId, string? color, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteAsync(Guid tagId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
