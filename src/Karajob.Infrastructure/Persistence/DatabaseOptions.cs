namespace Karajob.Infrastructure.Persistence;

public class DatabaseOptions
{
  public const string SectionName = "Database";
  public const string SqliteProvider = "Sqlite";
  public const string SqliteMigrationsAssembly = "Karajob.Migrations.Sqlite";

  public required string Provider { get; init; }
  public required string ConnectionString { get; init; }
}
