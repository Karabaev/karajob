using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Metrics;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.WeeklyReviews;

public class WeeklyReviewService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  HypothesisMetricsService metricsService)
{
  public async Task<WeeklyReviewSummary> GetAsync(CancellationToken cancellationToken = default)
  {
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var zone = currentUser.TimeZone;
    var week = CalendarWeek.Containing(now, zone);
    var activity = await metricsService.GetActivityAsync(now, cancellationToken);

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var maturationDays = await db.SearchGoals.AsNoTracking().Select(g => g.MaturationDays).SingleAsync(cancellationToken);

    var hypotheses = await db.Hypotheses.AsNoTracking()
      .Where(h => h.Status == HypothesisStatus.Active || h.Status == HypothesisStatus.Expanded)
      .Select(h => new { h.Id, h.Name, h.Status })
      .ToListAsync(cancellationToken);
    var checkedInIds = await db.HypothesisCheckIns.AsNoTracking()
      .Where(c => c.Week == week.FirstDay)
      .Select(c => c.HypothesisId)
      .ToListAsync(cancellationToken);
    var checkedIn = checkedInIds.ToHashSet();
    var reviewHypotheses = hypotheses
      .OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
      .Select(h => new WeeklyReviewHypothesis(h.Id, h.Name, h.Status, checkedIn.Contains(h.Id)))
      .ToList();

    var vacancies = await db.Vacancies.AsNoTracking()
      .Where(v => v.Stage == VacancyStage.Applied)
      .Include(v => v.Application)
      .Include(v => v.Events)
      .ToListAsync(cancellationToken);
    var stale = vacancies
      .Select(v => new { Vacancy = v, LastActivity = v.Events.Max(e => e.OccurredAt) })
      .Where(x => StaleApplicationRule.IsStale(x.LastActivity, now, maturationDays, zone))
      .ToList();

    var companyIds = stale.Where(x => x.Vacancy.CompanyId is not null).Select(x => x.Vacancy.CompanyId!.Value).Distinct().ToList();
    var roleIds = stale.Where(x => x.Vacancy.RoleId is not null).Select(x => x.Vacancy.RoleId!.Value).Distinct().ToList();
    var hypothesisIds = stale.Select(x => x.Vacancy.Application!.HypothesisId).Distinct().ToList();

    var companyNames = await db.Companies.AsNoTracking()
      .Where(c => companyIds.Contains(c.Id))
      .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    var roleNames = await db.Roles.AsNoTracking()
      .Where(r => roleIds.Contains(r.Id))
      .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    var hypothesisNames = await db.Hypotheses.AsNoTracking()
      .Where(h => hypothesisIds.Contains(h.Id))
      .ToDictionaryAsync(h => h.Id, h => h.Name, cancellationToken);

    var staleApplications = stale
      .Select(x =>
      {
        var v = x.Vacancy;
        var application = v.Application!;
        return new StaleApplication(
          v.Id,
          v.Url,
          v.Title,
          v.CompanyId is { } companyId ? companyNames[companyId] : null,
          v.RoleId is { } roleId ? roleNames[roleId] : null,
          application.HypothesisId,
          hypothesisNames[application.HypothesisId],
          application.AppliedAt,
          x.LastActivity,
          StaleApplicationRule.IdleDays(x.LastActivity, now, zone));
      })
      .OrderByDescending(s => s.IdleDays)
      .ThenBy(s => s.AppliedAt)
      .ToList();

    return new WeeklyReviewSummary(week, activity, maturationDays, reviewHypotheses, staleApplications);
  }
}
