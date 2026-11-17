using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Metrics;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Tracks;

public abstract class TrackDecisionServiceFixture : TrackMetricsServiceFixture
{
  protected static readonly DateOnly CurrentWeek = new(2026, 11, 9);
  protected static readonly DateOnly PreviousWeek = CurrentWeek.AddDays(-7);
  protected static readonly DateOnly TwoWeeksAgo = CurrentWeek.AddDays(-14);

  protected TrackDecisionService Decisions => DecisionsFor(SeedData.OwnerId);

  protected TrackDecisionService DecisionsFor(Guid userId, TimeZoneInfo? timeZone = null)
  {
    return TestDbContextFactory.CreateTrackDecisionService(Database, userId, Clock, timeZone);
  }

  protected static CheckInInput CheckIn(DateOnly day, int motivation = 3, int confidence = 3, string? note = null)
  {
    return new CheckInInput(day, motivation, confidence, note);
  }

  protected static DecisionInput Decision(
    TrackDecisionKind kind,
    string? reason = null,
    string? changes = null,
    Guid? activateDraftId = null)
  {
    return new DecisionInput(kind, reason, changes, activateDraftId);
  }

  protected async Task<Guid> CreateActiveAsync(string name = "Unity Developer RU", Guid? userId = null, TrackStatus status = TrackStatus.Active)
  {
    var previous = Clock.UtcNow;
    Clock.UtcNow = Now.AddDays(-60);
    try
    {
      var trackId = await CreateTrackAsync(name, userId, TrackStatus.Active);
      if (status != TrackStatus.Active)
      {
        await SetStatusAsync(trackId, status, userId);
      }

      return trackId;
    }
    finally
    {
      Clock.UtcNow = previous;
    }
  }

  protected async Task SetStatusAsync(Guid trackId, TrackStatus status, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var track = await context.Tracks.SingleAsync(h => h.Id == trackId);
    track.Status = status;
    await context.SaveChangesAsync();
  }

  protected async Task<Track> LoadTrackAsync(Guid trackId, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    return await context.Tracks.AsNoTracking().SingleAsync(h => h.Id == trackId);
  }

  protected async Task<int> CountCheckInsAsync(Guid trackId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.TrackCheckIns.CountAsync(c => c.TrackId == trackId);
  }

  protected async Task<int> CountDecisionsAsync(Guid trackId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.TrackDecisions.CountAsync(d => d.TrackId == trackId);
  }

  protected async Task<int> CountDecisionsAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.TrackDecisions.CountAsync();
  }

  protected async Task ApplyMaturedAsync(Guid trackId, int total, int positive)
  {
    var maturedAt = Now.AddDays(-20);
    for (var i = 0; i < total; i++)
    {
      var vacancyId = await ApplyAsync(trackId, maturedAt);
      if (i < positive)
      {
        await AddEventAsync(vacancyId, Karajob.Domain.Applications.ApplicationEventType.Screening, maturedAt.AddDays(2));
      }
    }
  }

  protected async Task CreateMarketVacanciesAsync(Guid trackId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await CreateVacancyAsync(trackId, Utc(11, 10, 9));
    }
  }

  protected async Task SetWellBeingThresholdAsync(int threshold, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.WellBeingThreshold = threshold;
    await context.SaveChangesAsync();
  }
}
