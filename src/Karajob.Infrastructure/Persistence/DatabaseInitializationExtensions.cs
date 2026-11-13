using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
  public static Task InitializeKarajobDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
  {
    return services.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
  }
}
