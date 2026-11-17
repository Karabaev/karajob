using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Metrics;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Hypotheses;

public abstract class HypothesisDecisionServiceFixture : HypothesisMetricsServiceFixture
{
  protected static readonly DateOnly CurrentWeek = new(2026, 11, 9);
  protected static readonly DateOnly PreviousWeek = CurrentWeek.AddDays(-7);
  protected static readonly DateOnly TwoWeeksAgo = CurrentWeek.AddDays(-14);

  protected HypothesisDecisionService Decisions => DecisionsFor(SeedData.OwnerId);

  protected HypothesisDecisionService DecisionsFor(Guid userId, TimeZoneInfo? timeZone = null)
  {
    return TestDbContextFactory.CreateHypothesisDecisionService(Database, userId, Clock, timeZone);
  }

  protected static CheckInInput CheckIn(DateOnly day, int motivation = 3, int confidence = 3, string? note = null)
  {
    return new CheckInInput(day, motivation, confidence, note);
  }

  protected static DecisionInput Decision(
    HypothesisDecisionKind kind,
    string? reason = null,
    string? changes = null,
    Guid? activateDraftId = null)
  {
    return new DecisionInput(kind, reason, changes, activateDraftId);
  }

  protected async Task<Guid> CreateActiveAsync(string name = "Unity Developer RU", Guid? userId = null, HypothesisStatus status = HypothesisStatus.Active)
  {
    var previous = Clock.UtcNow;
    Clock.UtcNow = Now.AddDays(-60);
    try
    {
      var hypothesisId = await CreateHypothesisAsync(name, userId, HypothesisStatus.Active);
      if (status != HypothesisStatus.Active)
      {
        await SetStatusAsync(hypothesisId, status, userId);
      }

      return hypothesisId;
    }
    finally
    {
      Clock.UtcNow = previous;
    }
  }

  protected async Task SetStatusAsync(Guid hypothesisId, HypothesisStatus status, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var hypothesis = await context.Hypotheses.SingleAsync(h => h.Id == hypothesisId);
    hypothesis.Status = status;
    await context.SaveChangesAsync();
  }

  protected async Task<Hypothesis> LoadHypothesisAsync(Guid hypothesisId, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    return await context.Hypotheses.AsNoTracking().SingleAsync(h => h.Id == hypothesisId);
  }

  protected async Task<int> CountCheckInsAsync(Guid hypothesisId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.HypothesisCheckIns.CountAsync(c => c.HypothesisId == hypothesisId);
  }

  protected async Task<int> CountDecisionsAsync(Guid hypothesisId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.HypothesisDecisions.CountAsync(d => d.HypothesisId == hypothesisId);
  }

  protected async Task<int> CountDecisionsAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.HypothesisDecisions.CountAsync();
  }

  protected async Task ApplyMaturedAsync(Guid hypothesisId, int total, int positive)
  {
    var maturedAt = Now.AddDays(-20);
    for (var i = 0; i < total; i++)
    {
      var vacancyId = await ApplyAsync(hypothesisId, maturedAt);
      if (i < positive)
      {
        await AddEventAsync(vacancyId, Karajob.Domain.Applications.ApplicationEventType.Screening, maturedAt.AddDays(2));
      }
    }
  }

  protected async Task CreateMarketVacanciesAsync(Guid hypothesisId, int count)
  {
    for (var i = 0; i < count; i++)
    {
      await CreateVacancyAsync(hypothesisId, Utc(11, 10, 9));
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
