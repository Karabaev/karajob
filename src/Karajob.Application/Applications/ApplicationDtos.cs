using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Application.Applications;

public record ApplicationHistoryFilter(VacancyStage? Stage, ApplicationOutcome? Outcome, string? Search);

public record ApplicationHistoryItem(
  Guid VacancyId,
  string Url,
  string? CompanyName,
  string? RoleName,
  VacancyStage Stage,
  ApplicationOutcome? Outcome,
  Guid HypothesisId,
  string HypothesisName,
  string CvName,
  string CvFileName,
  ApplicationChannel Channel,
  DateTime AppliedAt,
  DateTime? ClosedAt,
  string? CoverLetter,
  VacancyStage? ClosedFromStage,
  VacancyStage? ReopenTarget);
