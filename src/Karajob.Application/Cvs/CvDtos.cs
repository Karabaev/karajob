namespace Karajob.Application.Cvs;

public record CvInput(string Name, Guid? LanguageId, IReadOnlyList<string> Tags);

// OpenRead is deferred so the service validates name and size before the browser stream is opened.
public record CvUpload(string FileName, long SizeBytes, Func<Stream> OpenRead, string? Comment);

public record CvListItem(
  Guid Id,
  string Name,
  string LanguageCode,
  IReadOnlyList<string> Tags,
  int VersionCount,
  DateTime LatestVersionAt);

public record CvDetails(
  Guid Id,
  string Name,
  Guid LanguageId,
  IReadOnlyList<string> Tags,
  IReadOnlyList<CvVersionItem> Versions);

public record CvVersionItem(Guid Id, string FileName, long SizeBytes, string? Comment, DateTime CreatedAt);

public record CvFile(Stream Content, string FileName, string ContentType);
