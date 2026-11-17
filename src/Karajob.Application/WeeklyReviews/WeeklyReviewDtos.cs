using Karajob.Application.Metrics;
using Karajob.Domain.Tracks;
using Karajob.Domain.Metrics;

namespace Karajob.Application.WeeklyReviews;

public record WeeklyReviewSummary(
  CalendarWeek Week,
  ProfileActivity Activity,
  int StaleAfterDays,
  IReadOnlyList<WeeklyReviewTrack> Tracks,
  IReadOnlyList<StaleApplication> StaleApplications);

public record WeeklyReviewTrack(Guid Id, string Name, TrackStatus Status, bool CheckedInThisWeek);

public record StaleApplication(
  Guid VacancyId,
  string Url,
  string? Title,
  string? CompanyName,
  string? RoleName,
  Guid TrackId,
  string TrackName,
  DateTime AppliedAt,
  DateTime LastActivityAt,
  int IdleDays);
