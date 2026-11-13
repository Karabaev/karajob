using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karajob.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
  public static IServiceCollection AddKarajobPersistence(this IServiceCollection services, DatabaseOptions options)
  {
    if (options.Provider != DatabaseOptions.SqliteProvider)
    {
      throw new InvalidOperationException(
        $"Unsupported {DatabaseOptions.SectionName}:Provider '{options.Provider}'. Supported: {DatabaseOptions.SqliteProvider}.");
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionString);
    services.AddSingleton(options);
    services.TryAddSingleton(TimeProvider.System);
    services.AddSingleton<DatabaseInitializer>();
    services.AddDbContextFactory<KarajobDbContext>(builder => builder.UseSqlite(
      options.ConnectionString,
      sqlite => sqlite.MigrationsAssembly(DatabaseOptions.SqliteMigrationsAssembly)));
    return services;
  }
}
