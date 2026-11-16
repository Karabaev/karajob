using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Domain.Goals;
using Karajob.Domain.Normalization;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Goals;

public class SearchGoalService(IKarajobDbContextFactory dbContextFactory, IStringLocalizer<GoalMessages> localizer)
{
  public async Task<SearchGoalDetails> GetAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var goal = await db.SearchGoals.AsNoTracking().SingleAsync(cancellationToken);
    return ToDetails(goal);
  }

  public async Task<SearchGoalDetails> UpdateAsync(SearchGoalInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var goal = await db.SearchGoals.SingleAsync(cancellationToken);
    var currency = await ValidateAsync(db, input, cancellationToken);

    goal.RoleId = input.RoleId;
    goal.SetLocations(input.LocationIds);
    goal.SetPlatforms(input.PlatformIds);
    goal.SetEngines(input.EngineIds);
    goal.Industries = input.Industries;
    goal.MustHave = input.MustHave;
    goal.WeeklyApplicationTarget = input.WeeklyApplicationTarget;
    goal.Deadline = input.Deadline;
    goal.SetDesiredIncome(input.DesiredMonthlyIncome, currency);
    goal.MarketThresholdPerWeek = input.MarketThresholdPerWeek;
    goal.ConversionThresholdPermille = input.ConversionThresholdPermille;
    goal.MinSampleSize = input.MinSampleSize;
    goal.MaturationDays = input.MaturationDays;

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(goal);
  }

  private async Task<string?> ValidateAsync(IKarajobDbContext db, SearchGoalInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    if (input.RoleId is { } roleId && !await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(SearchGoalInput.RoleId)] = localizer["Error.RoleNotFound"];
    }

    var locationIds = input.LocationIds.Distinct().ToList();
    if (locationIds.Count > 0 && await db.Locations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken) != locationIds.Count)
    {
      errors[nameof(SearchGoalInput.LocationIds)] = localizer["Error.LocationNotFound"];
    }

    var platformIds = input.PlatformIds.Distinct().ToList();
    if (platformIds.Count > 0 && await db.Platforms.CountAsync(p => platformIds.Contains(p.Id), cancellationToken) != platformIds.Count)
    {
      errors[nameof(SearchGoalInput.PlatformIds)] = localizer["Error.PlatformNotFound"];
    }

    var engineIds = input.EngineIds.Distinct().ToList();
    if (engineIds.Count > 0 && await db.Engines.CountAsync(e => engineIds.Contains(e.Id), cancellationToken) != engineIds.Count)
    {
      errors[nameof(SearchGoalInput.EngineIds)] = localizer["Error.EngineNotFound"];
    }

    if (input.WeeklyApplicationTarget < 0)
    {
      errors[nameof(SearchGoalInput.WeeklyApplicationTarget)] = localizer["Error.WeeklyTargetNegative"];
    }

    if (input.MarketThresholdPerWeek < 0)
    {
      errors[nameof(SearchGoalInput.MarketThresholdPerWeek)] = localizer["Error.MarketThresholdNegative"];
    }

    if (input.ConversionThresholdPermille is < 0 or > SearchGoal.MaxConversionThresholdPermille)
    {
      errors[nameof(SearchGoalInput.ConversionThresholdPermille)] = localizer["Error.ConversionThresholdRange"];
    }

    if (input.MinSampleSize < SearchGoal.LowestMinSampleSize)
    {
      errors[nameof(SearchGoalInput.MinSampleSize)] = localizer["Error.MinSampleSize", SearchGoal.LowestMinSampleSize];
    }

    if (input.MaturationDays is < 0 or > SearchGoal.MaxMaturationDays)
    {
      errors[nameof(SearchGoalInput.MaturationDays)] = localizer["Error.MaturationDays", SearchGoal.MaxMaturationDays];
    }

    var currency = string.IsNullOrWhiteSpace(input.IncomeCurrency) ? null : input.IncomeCurrency;
    if (input.DesiredMonthlyIncome is { } income)
    {
      if (income <= 0)
      {
        errors[nameof(SearchGoalInput.DesiredMonthlyIncome)] = localizer["Error.IncomeNotPositive"];
      }

      if (currency is null)
      {
        errors[nameof(SearchGoalInput.IncomeCurrency)] = localizer["Error.CurrencyRequired"];
      }
    }
    else if (currency is not null)
    {
      errors[nameof(SearchGoalInput.DesiredMonthlyIncome)] = localizer["Error.IncomeRequired"];
    }

    string? code = null;
    if (currency is not null)
    {
      try
      {
        code = Normalize.CurrencyCode(currency);
      }
      catch (ArgumentException)
      {
        errors[nameof(SearchGoalInput.IncomeCurrency)] = localizer["Error.CurrencyCodeInvalid"];
      }
    }

    if (code is not null && !await db.Currencies.AnyAsync(c => c.Code == code, cancellationToken))
    {
      errors[nameof(SearchGoalInput.IncomeCurrency)] = localizer["Error.CurrencyNotFound"];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return code;
  }

  private static SearchGoalDetails ToDetails(SearchGoal goal)
  {
    return new SearchGoalDetails(
      goal.RoleId,
      goal.Locations.Select(l => l.LocationId).ToList(),
      goal.Platforms.Select(p => p.PlatformId).ToList(),
      goal.Engines.Select(e => e.EngineId).ToList(),
      goal.Industries,
      goal.MustHave,
      goal.WeeklyApplicationTarget,
      goal.Deadline,
      goal.DesiredMonthlyIncome,
      goal.IncomeCurrency,
      goal.MarketThresholdPerWeek,
      goal.ConversionThresholdPermille,
      goal.MinSampleSize,
      goal.MaturationDays);
  }
}
