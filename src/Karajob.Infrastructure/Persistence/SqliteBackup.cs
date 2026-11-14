using Microsoft.Data.Sqlite;

namespace Karajob.Infrastructure.Persistence;

public static class SqliteBackup
{
  private const string TempExtension = ".tmp";

  public static void Copy(string sourceConnectionString, string destinationPath)
  {
    // Written aside and renamed when complete, so backup tools never pick up a half-written copy.
    var tempPath = destinationPath + TempExtension;
    try
    {
      using var source = new SqliteConnection(sourceConnectionString);
      // Unpooled, so the copy is not kept open after the backup and can be renamed or deleted.
      using var destination = new SqliteConnection(
        new SqliteConnectionStringBuilder { DataSource = tempPath, Pooling = false }.ToString());
      source.Open();
      destination.Open();
      source.BackupDatabase(destination);
    }
    catch
    {
      // Both connections are closed here, so the partial copy is no longer locked.
      File.Delete(tempPath);
      throw;
    }

    File.Move(tempPath, destinationPath);
  }
}
