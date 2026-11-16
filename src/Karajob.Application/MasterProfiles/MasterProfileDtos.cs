namespace Karajob.Application.MasterProfiles;

public record MasterProfileInput(string Markdown, string? Comment);

public record MasterProfileDetails(
  string Markdown,
  Guid CurrentVersionId,
  IReadOnlyList<MasterProfileVersionSummary> Versions);

public record MasterProfileVersionSummary(Guid Id, DateTime CreatedAt, string? Comment, Guid? RestoredFromVersionId);

public record MasterProfileVersionDto(Guid Id, string Markdown, DateTime CreatedAt, string? Comment, Guid? RestoredFromVersionId);
