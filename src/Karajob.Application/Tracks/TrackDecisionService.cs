using Karajob.Application.Common;
using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Resources;
using Karajob.Application.Users;
using Karajob.Domain.Common;
using Karajob.Domain.Tracks;
using Karajob.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Karajob.Application.Tracks;

public class TrackDecisionService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  TrackMetricsService metricsService,
  IStringLocalizer<TrackMessages> localizer)
{
  public async Task<CheckInDetails> SaveCheckInAsync(Guid trackId, CheckInInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var status = await FindStatusAsync(db, trackId, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var currentWeek = CalendarWeek.Containing(now, currentUser.TimeZone).FirstDay;
    var week = CalendarWeek.MondayOf(input.Day);

    var errors = new Dictionary<string, string>();
    if (!ActiveTracksRule.IsActive(status))
    {
      errors[string.Empty] = localizer["Error.StatusNotAllowed"];
    }

    if (week > currentWeek)
    {
      errors[nameof(CheckInInput.Day)] = localizer["Error.CheckInFuture"];
    }

    if (input.Motivation is < TrackCheckIn.MinScore or > TrackCheckIn.MaxScore)
    {
      errors[nameof(CheckInInput.Motivation)] = localizer["Error.MotivationRange", TrackCheckIn.MinScore, TrackCheckIn.MaxScore];
    }

    if (input.Confidence is < TrackCheckIn.MinScore or > TrackCheckIn.MaxScore)
    {
      errors[nameof(CheckInInput.Confidence)] = localizer["Error.ConfidenceRange", TrackCheckIn.MinScore, TrackCheckIn.MaxScore];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    var checkIn = await db.TrackCheckIns.FirstOrDefaultAsync(c => c.TrackId == trackId && c.Week == week, cancellationToken);
    if (checkIn is null)
    {
      checkIn = new TrackCheckIn(currentUser.UserId, trackId, week, input.Motivation, input.Confidence, input.Note, now);
      db.TrackCheckIns.Add(checkIn);
    }
    else
    {
      checkIn.Update(input.Motivation, input.Confidence, input.Note, now);
    }

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(checkIn);
  }

  public async Task<IReadOnlyList<CheckInDetails>> ListCheckInsAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var checkIns = await db.TrackCheckIns.AsNoTracking()
      .Where(c => c.TrackId == trackId)
      .OrderByDescending(c => c.Week)
      .ToListAsync(cancellationToken);
    return checkIns.Select(ToDetails).ToList();
  }

  public async Task<TrackReview> GetReviewAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindStatusAsync(db, trackId, cancellationToken);
    return await BuildReviewAsync(db, trackId, cancellationToken);
  }

  public async Task<DisableOptions> GetDisableOptionsAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindStatusAsync(db, trackId, cancellationToken);

    var drafts = await db.Tracks.AsNoTracking()
      .Where(h => h.Status == TrackStatus.Draft)
      .Select(h => new DraftOption(h.Id, h.Name))
      .ToListAsync(cancellationToken);
    var activeCountAfter = await db.Tracks.AsNoTracking()
      .CountAsync(h => h.Id != trackId && (h.Status == TrackStatus.Active || h.Status == TrackStatus.Expanded), cancellationToken);

    return new DisableOptions(drafts.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList(), activeCountAfter);
  }

  public async Task<DecisionResult> RecordDecisionAsync(Guid trackId, DecisionInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var track = await db.Tracks.FirstOrDefaultAsync(h => h.Id == trackId, cancellationToken)
      ?? throw new KeyNotFoundException($"Track {trackId} was not found.");

    var errors = new Dictionary<string, string>();
    if (!ActiveTracksRule.IsActive(track.Status))
    {
      errors[string.Empty] = localizer["Error.StatusNotAllowed"];
    }

    if (input.Kind == TrackDecisionKind.Expand && string.IsNullOrWhiteSpace(input.Changes))
    {
      errors[nameof(DecisionInput.Changes)] = localizer["Error.ChangesRequired"];
    }

    if (input.Kind == TrackDecisionKind.Disable && string.IsNullOrWhiteSpace(input.Reason))
    {
      errors[nameof(DecisionInput.Reason)] = localizer["Error.ReasonRequired"];
    }

    Track? draft = null;
    if (input.ActivateDraftId is { } draftId)
    {
      if (input.Kind != TrackDecisionKind.Disable)
      {
        errors[nameof(DecisionInput.ActivateDraftId)] = localizer["Error.DraftOnlyOnDisable"];
      }
      else
      {
        draft = await db.Tracks.FirstOrDefaultAsync(h => h.Id == draftId && h.Status == TrackStatus.Draft, cancellationToken);
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

    var review = await BuildReviewAsync(db, trackId, cancellationToken);
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

    track.ApplyDecision(input.Kind, now);
    draft?.ChangeStatus(TrackStatus.Active, now);
    var decision = new TrackDecision(currentUser.UserId, trackId, now, input.Kind, input.Reason, input.Changes, snapshot);
    db.TrackDecisions.Add(decision);
    await db.SaveChangesAsync(cancellationToken);

    var activeCount = await db.Tracks.AsNoTracking()
      .CountAsync(h => h.Status == TrackStatus.Active || h.Status == TrackStatus.Expanded, cancellationToken);
    return new DecisionResult(ToDetails(decision), track.Status, activeCount, ActiveTracksRule.Check(activeCount));
  }

  public async Task<IReadOnlyList<DecisionDetails>> ListDecisionsAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var decisions = await db.TrackDecisions.AsNoTracking()
      .Where(d => d.TrackId == trackId)
      .OrderByDescending(d => d.DecidedAt)
      .ToListAsync(cancellationToken);
    return decisions.Select(ToDetails).ToList();
  }

  private async Task<TrackReview> BuildReviewAsync(IKarajobDbContext db, Guid trackId, CancellationToken cancellationToken)
  {
    var metrics = await metricsService.GetAsync(trackId, cancellationToken: cancellationToken);
    var threshold = await db.SearchGoals.AsNoTracking().Select(g => g.WellBeingThreshold).SingleAsync(cancellationToken);
    var checkIns = await db.TrackCheckIns.AsNoTracking()
      .Where(c => c.TrackId == trackId)
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
    return new TrackReview(metrics, wellBeing, threshold, hint);
  }

  private static async Task<TrackStatus> FindStatusAsync(IKarajobDbContext db, Guid trackId, CancellationToken cancellationToken)
  {
    var statuses = await db.Tracks.AsNoTracking()
      .Where(h => h.Id == trackId)
      .Select(h => (TrackStatus?)h.Status)
      .ToListAsync(cancellationToken);
    return statuses.SingleOrDefault() ?? throw new KeyNotFoundException($"Track {trackId} was not found.");
  }

  private static CheckInDetails ToDetails(TrackCheckIn c)
  {
    return new CheckInDetails(c.Id, c.Week, c.Motivation, c.Confidence, c.Note, WellBeing.AverageTenths(c.Motivation, c.Confidence));
  }

  private static DecisionDetails ToDetails(TrackDecision d)
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
