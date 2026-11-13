namespace Karajob.Infrastructure.Persistence;

public class DatabaseMigrationException(string message, string? backupPath, Exception innerException)
  : Exception(message, innerException)
{
  public string? BackupPath { get; } = backupPath;
}
