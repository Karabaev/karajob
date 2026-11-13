using Karajob.Application.Persistence;

namespace Karajob.Application.Catalogs;

public class CatalogService(IKarajobDbContextFactory dbContextFactory)
{
  public Task<IReadOnlyList<CatalogListItem>> ListAsync(CatalogKind kind, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CatalogListItem> CreateAsync(CatalogKind kind, CatalogInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CatalogListItem> RenameAsync(CatalogKind kind, Guid id, string name, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteAsync(CatalogKind kind, Guid id, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
