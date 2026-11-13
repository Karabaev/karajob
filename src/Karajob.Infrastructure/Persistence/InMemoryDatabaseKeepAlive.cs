using Microsoft.Data.Sqlite;

namespace Karajob.Infrastructure.Persistence;

// A shared-cache in-memory SQLite database is dropped when its last connection closes.
public class InMemoryDatabaseKeepAlive : IDisposable
{
  private readonly SqliteConnection connection;

  public void Dispose()
  {
    connection.Dispose();
  }

  public InMemoryDatabaseKeepAlive(string connectionString)
  {
    connection = new SqliteConnection(connectionString);
    connection.Open();
  }
}
