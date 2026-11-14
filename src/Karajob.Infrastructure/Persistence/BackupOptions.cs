namespace Karajob.Infrastructure.Persistence;

public class BackupOptions
{
  public const string SectionName = "Backup";

  public required int KeepDaily { get; init; }
}
