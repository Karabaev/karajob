using Karajob.Domain.Hypotheses;

namespace Karajob.Application.Hypotheses;

public record HypothesisInput(
  string Name,
  Guid? RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  Guid? LanguageId,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  HypothesisStatus Status);

public record HypothesisListItem(
  Guid Id,
  string Name,
  string Role,
  WorkFormat WorkFormat,
  string LanguageCode,
  HypothesisStatus Status);

public record HypothesisDetails(
  Guid Id,
  string Name,
  Guid RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  Guid LanguageId,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  HypothesisStatus Status,
  DateTime? StartedAt);

public record CvOption(Guid Id, string Name, string LanguageCode);
