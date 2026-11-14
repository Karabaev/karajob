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
  DateTime DiscoveredAt,
  string? Notes);

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

public record QuickAddInput(string Url, string? Note);

public record QuickAddResult(Guid VacancyId, bool AlreadyExisted, VacancyStage Stage);

public record DismissInput(DismissReason Reason, string? Comment);

public record BoardCard(
  Guid Id,
  string Url,
  string? CompanyName,
  string? RoleName,
  VacancyStage Stage,
  ApplicationOutcome? Outcome,
  DateTime DiscoveredAt);

public record BoardView(IReadOnlyList<BoardCard> Cards, IReadOnlyDictionary<ApplicationOutcome, int> ClosedOutcomes);

public record ApplyInput(Guid HypothesisId, Guid CvVersionId, ApplicationChannel Channel, DateTime AppliedAt, string? CoverLetter);

public record ApplyOptions(IReadOnlyList<ApplyHypothesisOption> Hypotheses, IReadOnlyList<CvVersionOption> CvVersions);

public record ApplyHypothesisOption(Guid Id, string Name, Guid? DefaultCvVersionId);

public record CvVersionOption(Guid Id, string CvName, string FileName, DateTime CreatedAt);
