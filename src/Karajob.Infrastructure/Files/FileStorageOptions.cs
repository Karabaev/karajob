namespace Karajob.Infrastructure.Files;

public class FileStorageOptions
{
  public const string SectionName = "Files";

  public required string RootDirectory { get; init; }
}
