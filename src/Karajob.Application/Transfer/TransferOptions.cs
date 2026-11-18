namespace Karajob.Application.Transfer;

public class TransferOptions
{
  public const string SectionName = "Transfer";

  public required int MaxImportSizeMb { get; init; }
  public long MaxImportSizeBytes => MaxImportSizeMb * 1024L * 1024;
}
