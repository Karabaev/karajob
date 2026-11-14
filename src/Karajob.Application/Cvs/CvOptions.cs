namespace Karajob.Application.Cvs;

public class CvOptions
{
  public const string SectionName = "Cv";

  public required int MaxFileSizeMb { get; init; }
  public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024;
}
