using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
  public static async Task InitializeKarajobDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
  {
    await using var scope = services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
  }
}
