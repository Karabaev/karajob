using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karajob.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
  public static IServiceCollection AddKarajobPersistence(this IServiceCollection services, DatabaseOptions options)
  {
    switch (options.Provider)
    {
      case DatabaseOptions.SqliteProvider:
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionString);
        AddSqliteContextFactory(services, options.ConnectionString, keepAlive: false);
        break;
      case DatabaseOptions.SqliteInMemoryProvider:
        var connectionString = new SqliteConnectionStringBuilder
        {
          // Unique per registration so parallel hosts (e.g. test factories) never share data.
          DataSource = $"karajob-{Guid.NewGuid():N}",
          Mode = SqliteOpenMode.Memory,
          Cache = SqliteCacheMode.Shared,
        }.ToString();
        services.AddSingleton(_ => new InMemoryDatabaseKeepAlive(connectionString));
        AddSqliteContextFactory(services, connectionString, keepAlive: true);
        break;
      default:
        throw new InvalidOperationException(
          $"Unsupported {DatabaseOptions.SectionName}:Provider '{options.Provider}'. " +
          $"Supported: {DatabaseOptions.SqliteProvider}, {DatabaseOptions.SqliteInMemoryProvider}.");
    }

    services.AddSingleton(options);
    services.TryAddSingleton(TimeProvider.System);
    services.AddSingleton<DatabaseInitializer>();
    return services;
  }

  private static void AddSqliteContextFactory(IServiceCollection services, string connectionString, bool keepAlive)
  {
    services.AddDbContextFactory<KarajobDbContext>((serviceProvider, builder) =>
    {
      if (keepAlive)
      {
        serviceProvider.GetRequiredService<InMemoryDatabaseKeepAlive>();
      }

      builder.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(DatabaseOptions.SqliteMigrationsAssembly));
    });
  }
}
