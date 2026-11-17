using Karajob.Domain.Tracks;

namespace Karajob.Application.Tracks;

public record TrackInput(
  string Name,
  Guid? RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<Guid> PlatformIds,
  IReadOnlyList<Guid> EngineIds,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  Guid? LanguageId,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  TrackStatus Status);

public record TrackListItem(
  Guid Id,
  string Name,
  Guid RoleId,
  string Role,
  WorkFormat WorkFormat,
  string LanguageCode,
  TrackStatus Status,
  IReadOnlyList<string> PlatformNames);

public record TrackDetails(
  Guid Id,
  string Name,
  Guid RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<Guid> PlatformIds,
  IReadOnlyList<Guid> EngineIds,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  Guid LanguageId,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  TrackStatus Status,
  DateTime? StartedAt,
  DateTime? PeriodStartedAt);

public record CvOption(Guid Id, string Name, string LanguageCode);
