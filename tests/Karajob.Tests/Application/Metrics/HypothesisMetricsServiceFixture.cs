using Karajob.Application.Goals;
using Karajob.Application.Metrics;
using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Application.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Metrics;

public abstract class HypothesisMetricsServiceFixture : VacancyServiceFixture
{
  private readonly Dictionary<Guid, Guid> cvVersionIds = [];
  private int urlCounter;

  protected static readonly DateTime WeekStart = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

  protected HypothesisMetricsService Metrics => MetricsFor(SeedData.OwnerId);

  protected static DateTime Utc(int month, int day, int hour = 0, int minute = 0, int second = 0)
  {
    return new DateTime(2026, month, day, hour, minute, second, DateTimeKind.Utc);
  }

  protected HypothesisMetricsService MetricsInZone(string timeZoneId)
  {
    return TestDbContextFactory.CreateHypothesisMetricsService(Database, SeedData.OwnerId, Clock, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
  }

  protected HypothesisMetricsService MetricsFor(Guid userId)
  {
    return TestDbContextFactory.CreateHypothesisMetricsService(Database, userId, Clock);
  }

  protected async Task<Guid> CreateStartedHypothesisAsync(Guid? userId = null, string name = "Unity Developer RU")
  {
    var hypothesisId = await CreateHypothesisAsync(name, userId);
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var hypothesis = await context.Hypotheses.SingleAsync(h => h.Id == hypothesisId);
    hypothesis.StartedAt = Now.AddDays(-60);
    await context.SaveChangesAsync();
    return hypothesisId;
  }

  protected async Task<Guid> CreateHypothesisCreatedAtAsync(DateTime createdAt)
  {
    var previous = Clock.UtcNow;
    Clock.UtcNow = createdAt;
    try
    {
      return await CreateHypothesisAsync();
    }
    finally
    {
      Clock.UtcNow = previous;
    }
  }

  protected async Task<Guid> CreateVacancyAsync(Guid hypothesisId, DateTime discoveredAt, Guid? userId = null)
  {
    var service = userId is null ? Service : ServiceFor(userId.Value);
    var previous = Clock.UtcNow;
    Clock.UtcNow = discoveredAt;
    try
    {
      var vacancy = await service.CreateAsync(Input($"https://example.com/jobs/metrics-{++urlCounter}", hypothesisIds: [hypothesisId]));
      return vacancy.Id;
    }
    finally
    {
      Clock.UtcNow = previous;
    }
  }

  protected async Task<Guid> ApplyAsync(
    Guid hypothesisId,
    DateTime appliedAt,
    Guid? userId = null,
    Action<Vacancy>? afterApply = null)
  {
    var vacancyId = await CreateVacancyAsync(hypothesisId, appliedAt.AddDays(-3), userId);
    var cvVersionId = await GetCvVersionIdAsync(userId ?? SeedData.OwnerId);
    await MutateAsync(vacancyId, v =>
    {
      v.MoveTo(VacancyStage.Targeted, v.DiscoveredAt);
      v.Apply(hypothesisId, cvVersionId, ApplicationChannel.Website, appliedAt, null);
      afterApply?.Invoke(v);
    }, userId);
    return vacancyId;
  }

  protected Task AddEventAsync(Guid vacancyId, ApplicationEventType type, DateTime at)
  {
    return MutateAsync(vacancyId, v => v.AddEvent(type, at, null));
  }

  protected async Task<Guid> CreateProfileAsync()
  {
    var users = TestDbContextFactory.CreateUserService(Database, SeedData.OwnerId, new FixedTimeProvider(Now));
    var profile = await users.CreateAsync(new UserInput("Second", "#26a69a"));
    return profile.Id;
  }

  protected async Task UpdateGoalAsync(
    Guid? userId = null,
    int weeklyApplicationTarget = 10,
    int marketThresholdPerWeek = 10,
    int conversionThresholdPermille = 30,
    int minSampleSize = 30,
    int maturationDays = 14)
  {
    var goals = TestDbContextFactory.CreateSearchGoalService(Database, userId ?? SeedData.OwnerId);
    await goals.UpdateAsync(new SearchGoalInput(
      null,
      [],
      [],
      [],
      null,
      null,
      weeklyApplicationTarget,
      null,
      null,
      null,
      marketThresholdPerWeek,
      conversionThresholdPermille,
      minSampleSize,
      maturationDays));
  }

  private async Task<Guid> GetCvVersionIdAsync(Guid userId)
  {
    if (!cvVersionIds.TryGetValue(userId, out var versionId))
    {
      versionId = (await CreateCvAsync("Metrics CV", "metrics.pdf", userId == SeedData.OwnerId ? null : userId)).VersionId;
      cvVersionIds[userId] = versionId;
    }

    return versionId;
  }
}
