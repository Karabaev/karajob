namespace Karajob.Application.Persistence;

public interface IKarajobDbContextFactory
{
  Task<IKarajobDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
