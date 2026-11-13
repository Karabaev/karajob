using Karajob.Domain.Hypotheses;

namespace Karajob.Application.Hypotheses;

public record HypothesisInput(
  string Name,
  string Role,
  IReadOnlyList<string> Locations,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  string Language,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  HypothesisStatus Status);

public record HypothesisListItem(
  Guid Id,
  string Name,
  string Role,
  WorkFormat WorkFormat,
  string Language,
  HypothesisStatus Status);

public record HypothesisDetails(
  Guid Id,
  string Name,
  string Role,
  IReadOnlyList<string> Locations,
  IReadOnlyList<string> Industries,
  WorkFormat WorkFormat,
  string Language,
  string? Positioning,
  Guid? PrimaryCvDocumentId,
  HypothesisStatus Status,
  DateTime? StartedAt);

public record CvOption(Guid Id, string Name, string Language);
