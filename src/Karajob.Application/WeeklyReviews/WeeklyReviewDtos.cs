using Karajob.Application.Metrics;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Metrics;

namespace Karajob.Application.WeeklyReviews;

public record WeeklyReviewSummary(
  CalendarWeek Week,
  ProfileActivity Activity,
  int StaleAfterDays,
  IReadOnlyList<WeeklyReviewHypothesis> Hypotheses,
  IReadOnlyList<StaleApplication> StaleApplications);

public record WeeklyReviewHypothesis(Guid Id, string Name, HypothesisStatus Status, bool CheckedInThisWeek);

public record StaleApplication(
  Guid VacancyId,
  string Url,
  string? Title,
  string? CompanyName,
  string? RoleName,
  Guid HypothesisId,
  string HypothesisName,
  DateTime AppliedAt,
  DateTime LastActivityAt,
  int IdleDays);
