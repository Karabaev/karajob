using Karajob.Application.Persistence;
using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Metrics;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Metrics;

public class HypothesisMetricsService(IKarajobDbContextFactory dbContextFactory, TimeProvider timeProvider)
{
  private static readonly ApplicationEventType[] PositiveEventTypes =
  [
    ApplicationEventType.Screening,
    ApplicationEventType.TechInterview,
    ApplicationEventType.TestTask,
    ApplicationEventType.Offer,
  ];

  public async Task<HypothesisMetrics> GetAsync(
    Guid hypothesisId,
    DateTime? periodStart = null,
    DateTime? asOf = null,
    CancellationToken cancellationToken = default)
  {
    UtcGuard.Ensure(periodStart);
    var now = ResolveAsOf(asOf);

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.AsNoTracking()
      .Where(h => h.Id == hypothesisId)
      .Select(h => new { h.StartedAt, h.CreatedAt })
      .SingleOrDefaultAsync(cancellationToken)
      ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");
    var start = periodStart ?? hypothesis.StartedAt ?? hypothesis.CreatedAt;
    if (start > now) throw new ArgumentException("The period cannot start after the metrics date.", nameof(periodStart));

    var goal = await db.SearchGoals.AsNoTracking().SingleAsync(cancellationToken);
    var week = UtcWeek.Containing(now);
    var weekFrom = start > week.Start ? start : week.Start;

    var market = await db.VacancyMatches.AsNoTracking()
      .Where(m => m.HypothesisId == hypothesisId)
      .Join(db.Vacancies, m => m.VacancyId, v => v.Id, (m, v) => v.DiscoveredAt)
      .CountAsync(at => at >= weekFrom && at < week.End && at <= now, cancellationToken);

    var applications = await db.JobApplications.AsNoTracking()
      .Where(a => a.HypothesisId == hypothesisId && a.AppliedAt >= start && a.AppliedAt <= now)
      .Select(a => new { a.VacancyId, a.AppliedAt, a.Outcome })
      .ToListAsync(cancellationToken);

    var weeklyApplications = applications.Count(a => a.AppliedAt >= weekFrom && a.AppliedAt < week.End);

    var maturedBefore = now.AddDays(-goal.MaturationDays);
    var matured = applications.Where(a => a.AppliedAt <= maturedBefore).ToList();
    var maturedVacancyIds = matured.Select(a => a.VacancyId).ToList();
    var positiveEvents = await db.ApplicationEvents.AsNoTracking()
      .Where(e => maturedVacancyIds.Contains(e.VacancyId) && PositiveEventTypes.Contains(e.Type) && e.OccurredAt <= now)
      .Select(e => new { e.VacancyId, e.OccurredAt })
      .ToListAsync(cancellationToken);

    // Compared by day: the event dialog stores a past day as midnight, while the application keeps its time.
    var positive = matured.Count(a =>
      a.Outcome == ApplicationOutcome.Offer
      || positiveEvents.Any(e => e.VacancyId == a.VacancyId && e.OccurredAt.Date >= a.AppliedAt.Date));

    var conversion = new ConversionMetric(
      positive,
      matured.Count,
      matured.Count == 0 ? null : positive / (double)matured.Count,
      matured.Count == 0 ? null : WilsonInterval.Compute(positive, matured.Count),
      goal.ConversionThresholdPermille,
      goal.MinSampleSize,
      goal.MaturationDays,
      matured.Count < goal.MinSampleSize);

    return new HypothesisMetrics(
      hypothesisId,
      start,
      now,
      week,
      new MarketMetric(market, goal.MarketThresholdPerWeek),
      conversion,
      weeklyApplications);
  }

  public async Task<ProfileActivity> GetActivityAsync(DateTime? asOf = null, CancellationToken cancellationToken = default)
  {
    var now = ResolveAsOf(asOf);
    var week = UtcWeek.Containing(now);

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var goal = await db.SearchGoals.AsNoTracking().SingleAsync(cancellationToken);
    var applications = await db.JobApplications.AsNoTracking()
      .CountAsync(a => a.AppliedAt >= week.Start && a.AppliedAt < week.End && a.AppliedAt <= now, cancellationToken);
    return new ProfileActivity(week, applications, goal.WeeklyApplicationTarget);
  }

  private DateTime ResolveAsOf(DateTime? asOf)
  {
    return UtcGuard.Ensure(asOf) ?? timeProvider.GetUtcNow().UtcDateTime;
  }
}
