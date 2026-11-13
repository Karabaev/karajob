using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class SqliteTestDatabase : IAsyncDisposable
{
  private readonly SqliteConnection connection;
  private readonly DbContextOptions<KarajobDbContext> options;

  public static async Task<SqliteTestDatabase> CreateAsync()
  {
    var database = new SqliteTestDatabase();
    await using var context = database.CreateContext(SeedData.OwnerId);
    await context.Database.MigrateAsync();
    return database;
  }

  public KarajobDbContext CreateContext(Guid userId)
  {
    return new KarajobDbContext(options, new FixedCurrentUser(userId));
  }

  public async ValueTask DisposeAsync()
  {
    await connection.DisposeAsync();
  }

  private SqliteTestDatabase()
  {
    connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    options = new DbContextOptionsBuilder<KarajobDbContext>()
      .UseSqlite(connection, b => b.MigrationsAssembly(DatabaseOptions.SqliteMigrationsAssembly))
      .Options;
  }
}
