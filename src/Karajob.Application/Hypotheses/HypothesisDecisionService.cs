using Karajob.Application.Common;
using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Resources;
using Karajob.Application.Users;
using Karajob.Domain.Common;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Karajob.Application.Hypotheses;

public class HypothesisDecisionService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  HypothesisMetricsService metricsService,
  IStringLocalizer<HypothesisMessages> localizer)
{
  public async Task<CheckInDetails> SaveCheckInAsync(Guid hypothesisId, CheckInInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var status = await FindStatusAsync(db, hypothesisId, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var currentWeek = CalendarWeek.Containing(now, currentUser.TimeZone).FirstDay;
    var week = CalendarWeek.Containing(UserTime.StartOfDayUtc(input.Day, currentUser.TimeZone), currentUser.TimeZone).FirstDay;

    var errors = new Dictionary<string, string>();
    if (!ActiveHypothesesRule.IsActive(status))
    {
      errors[string.Empty] = localizer["Error.StatusNotAllowed"];
    }

    if (week > currentWeek)
    {
      errors[nameof(CheckInInput.Day)] = localizer["Error.CheckInFuture"];
    }

    if (input.Motivation is < HypothesisCheckIn.MinScore or > HypothesisCheckIn.MaxScore)
    {
      errors[nameof(CheckInInput.Motivation)] = localizer["Error.MotivationRange", HypothesisCheckIn.MinScore, HypothesisCheckIn.MaxScore];
    }

    if (input.Confidence is < HypothesisCheckIn.MinScore or > HypothesisCheckIn.MaxScore)
    {
      errors[nameof(CheckInInput.Confidence)] = localizer["Error.ConfidenceRange", HypothesisCheckIn.MinScore, HypothesisCheckIn.MaxScore];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    var checkIn = await db.HypothesisCheckIns.FirstOrDefaultAsync(c => c.HypothesisId == hypothesisId && c.Week == week, cancellationToken);
    if (checkIn is null)
    {
      checkIn = new HypothesisCheckIn(currentUser.UserId, hypothesisId, week, input.Motivation, input.Confidence, input.Note, now);
      db.HypothesisCheckIns.Add(checkIn);
    }
    else
    {
      checkIn.Update(input.Motivation, input.Confidence, input.Note, now);
    }

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(checkIn);
  }

  public async Task<IReadOnlyList<CheckInDetails>> ListCheckInsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var checkIns = await db.HypothesisCheckIns.AsNoTracking()
      .Where(c => c.HypothesisId == hypothesisId)
      .OrderByDescending(c => c.Week)
      .ToListAsync(cancellationToken);
    return checkIns.Select(ToDetails).ToList();
  }

  public async Task<HypothesisReview> GetReviewAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindStatusAsync(db, hypothesisId, cancellationToken);
    return await BuildReviewAsync(db, hypothesisId, cancellationToken);
  }

  public async Task<DisableOptions> GetDisableOptionsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindStatusAsync(db, hypothesisId, cancellationToken);

    var drafts = await db.Hypotheses.AsNoTracking()
      .Where(h => h.Status == HypothesisStatus.Draft)
      .Select(h => new DraftOption(h.Id, h.Name))
      .ToListAsync(cancellationToken);
    var activeCountAfter = await db.Hypotheses.AsNoTracking()
      .CountAsync(h => h.Id != hypothesisId && (h.Status == HypothesisStatus.Active || h.Status == HypothesisStatus.Expanded), cancellationToken);

    return new DisableOptions(drafts.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList(), activeCountAfter);
  }

  public async Task<DecisionResult> RecordDecisionAsync(Guid hypothesisId, DecisionInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.FirstOrDefaultAsync(h => h.Id == hypothesisId, cancellationToken)
      ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");

    var errors = new Dictionary<string, string>();
    if (!ActiveHypothesesRule.IsActive(hypothesis.Status))
    {
      errors[string.Empty] = localizer["Error.StatusNotAllowed"];
    }

    if (input.Kind == HypothesisDecisionKind.Expand && string.IsNullOrWhiteSpace(input.Changes))
    {
      errors[nameof(DecisionInput.Changes)] = localizer["Error.ChangesRequired"];
    }

    if (input.Kind == HypothesisDecisionKind.Disable && string.IsNullOrWhiteSpace(input.Reason))
    {
      errors[nameof(DecisionInput.Reason)] = localizer["Error.ReasonRequired"];
    }

    Hypothesis? draft = null;
    if (input.ActivateDraftId is { } draftId)
    {
      if (input.Kind != HypothesisDecisionKind.Disable)
      {
        errors[nameof(DecisionInput.ActivateDraftId)] = localizer["Error.DraftOnlyOnDisable"];
      }
      else
      {
        draft = await db.Hypotheses.FirstOrDefaultAsync(h => h.Id == draftId && h.Status == HypothesisStatus.Draft, cancellationToken);
        if (draft is null)
        {
          errors[nameof(DecisionInput.ActivateDraftId)] = localizer["Error.DraftNotFound"];
        }
      }
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    var review = await BuildReviewAsync(db, hypothesisId, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var metrics = review.Metrics;
    var snapshot = new DecisionSnapshot(
      metrics.PeriodStart,
      metrics.Market.VacanciesThisWeek,
      metrics.Market.ThresholdPerWeek,
      metrics.Conversion.Positive,
      metrics.Conversion.Matured,
      metrics.Conversion.ThresholdPermille,
      review.WellBeing.LatestTenths,
      review.Hint);

    hypothesis.ApplyDecision(input.Kind, now);
    draft?.ChangeStatus(HypothesisStatus.Active, now);
    var decision = new HypothesisDecision(currentUser.UserId, hypothesisId, now, input.Kind, input.Reason, input.Changes, snapshot);
    db.HypothesisDecisions.Add(decision);
    await db.SaveChangesAsync(cancellationToken);

    var activeCount = await db.Hypotheses.AsNoTracking()
      .CountAsync(h => h.Status == HypothesisStatus.Active || h.Status == HypothesisStatus.Expanded, cancellationToken);
    return new DecisionResult(ToDetails(decision), hypothesis.Status, activeCount, ActiveHypothesesRule.Check(activeCount));
  }

  public async Task<IReadOnlyList<DecisionDetails>> ListDecisionsAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var decisions = await db.HypothesisDecisions.AsNoTracking()
      .Where(d => d.HypothesisId == hypothesisId)
      .OrderByDescending(d => d.DecidedAt)
      .ToListAsync(cancellationToken);
    return decisions.Select(ToDetails).ToList();
  }

  private async Task<HypothesisReview> BuildReviewAsync(IKarajobDbContext db, Guid hypothesisId, CancellationToken cancellationToken)
  {
    var metrics = await metricsService.GetAsync(hypothesisId, cancellationToken: cancellationToken);
    var threshold = await db.SearchGoals.AsNoTracking().Select(g => g.WellBeingThreshold).SingleAsync(cancellationToken);
    var checkIns = await db.HypothesisCheckIns.AsNoTracking()
      .Where(c => c.HypothesisId == hypothesisId)
      .Select(c => new WellBeingCheckIn(c.Week, c.Motivation, c.Confidence))
      .ToListAsync(cancellationToken);
    var wellBeing = WellBeing.Summarize(checkIns, metrics.Week.FirstDay, threshold);
    var hint = DecisionTree.Suggest(new DecisionTreeInput(
      metrics.Market.VacanciesThisWeek,
      metrics.Market.ThresholdPerWeek,
      metrics.Conversion.Positive,
      metrics.Conversion.Matured,
      metrics.Conversion.ThresholdPermille,
      metrics.Conversion.MinSampleSize,
      wellBeing.IsLow));
    return new HypothesisReview(metrics, wellBeing, threshold, hint);
  }

  private static async Task<HypothesisStatus> FindStatusAsync(IKarajobDbContext db, Guid hypothesisId, CancellationToken cancellationToken)
  {
    var statuses = await db.Hypotheses.AsNoTracking()
      .Where(h => h.Id == hypothesisId)
      .Select(h => (HypothesisStatus?)h.Status)
      .ToListAsync(cancellationToken);
    return statuses.SingleOrDefault() ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");
  }

  private static CheckInDetails ToDetails(HypothesisCheckIn c)
  {
    return new CheckInDetails(c.Id, c.Week, c.Motivation, c.Confidence, c.Note, WellBeing.AverageTenths(c.Motivation, c.Confidence));
  }

  private static DecisionDetails ToDetails(HypothesisDecision d)
  {
    return new DecisionDetails(
      d.Id,
      d.DecidedAt,
      d.Kind,
      d.Reason,
      d.Changes,
      d.PeriodStartedAt,
      d.MarketVacancies,
      d.MarketThreshold,
      d.PositiveApplications,
      d.MaturedApplications,
      d.ConversionThresholdPermille,
      d.WellBeingTenths,
      d.Hint);
  }
}
