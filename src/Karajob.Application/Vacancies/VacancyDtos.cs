using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;

namespace Karajob.Application.Vacancies;

public record VacancyInput(
  string Url,
  Guid? CompanyId,
  Guid? RoleId,
  Guid? LocationId,
  Guid? LanguageId,
  WorkFormat? WorkFormat,
  long? SalaryMin,
  long? SalaryMax,
  string? SalaryCurrency,
  IReadOnlyList<Guid> HypothesisIds,
  string? Description,
  string? Notes);

public record VacancyListItem(
  Guid Id,
  string Url,
  string? CompanyName,
  string? RoleName,
  VacancyStage Stage,
  DateTime DiscoveredAt);

public record VacancyDetails(
  Guid Id,
  string Url,
  Guid? CompanyId,
  string? CompanyName,
  Guid? RoleId,
  Guid? LocationId,
  Guid? LanguageId,
  WorkFormat? WorkFormat,
  long? SalaryMin,
  long? SalaryMax,
  string? SalaryCurrency,
  IReadOnlyList<Guid> HypothesisIds,
  string? Description,
  string? Notes,
  VacancySource Source,
  VacancyStage Stage,
  DismissReason? DismissReason,
  DateTime DiscoveredAt,
  Guid? ApplicationHypothesisId,
  IReadOnlyList<TimelineItem> Timeline,
  IReadOnlyList<SimilarVacancy> Similar);

public record TimelineItem(
  Guid Id,
  ApplicationEventType Type,
  DateTime OccurredAt,
  VacancyStage? FromStage,
  VacancyStage? ToStage,
  string? Note);

public record SimilarVacancy(Guid Id, string Url, VacancyStage Stage, DateTime DiscoveredAt);

public record VacancyEventInput(ApplicationEventType Type, DateTime OccurredAt, string? Note);
