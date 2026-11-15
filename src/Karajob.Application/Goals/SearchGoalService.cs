using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Goals;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Goals;

public class SearchGoalService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser)
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

  private static async Task<string?> ValidateAsync(IKarajobDbContext db, SearchGoalInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    if (input.RoleId is { } roleId && !await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(SearchGoalInput.RoleId)] = "Роль не найдена";
    }

    var locationIds = input.LocationIds.Distinct().ToList();
    if (locationIds.Count > 0 && await db.Locations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken) != locationIds.Count)
    {
      errors[nameof(SearchGoalInput.LocationIds)] = "Локация не найдена";
    }

    if (input.WeeklyApplicationTarget < 0)
    {
      errors[nameof(SearchGoalInput.WeeklyApplicationTarget)] = "Цель по откликам не может быть отрицательной";
    }

    if (input.MarketThresholdPerWeek < 0)
    {
      errors[nameof(SearchGoalInput.MarketThresholdPerWeek)] = "Порог рынка не может быть отрицательным";
    }

    if (input.ConversionThresholdPermille is < 0 or > SearchGoal.MaxConversionThresholdPermille)
    {
      errors[nameof(SearchGoalInput.ConversionThresholdPermille)] = $"Порог конверсии — от 0 до {SearchGoal.MaxConversionThresholdPermille}‰";
    }

    if (input.MinSampleSize < SearchGoal.LowestMinSampleSize)
    {
      errors[nameof(SearchGoalInput.MinSampleSize)] = $"Минимальная выборка — не меньше {SearchGoal.LowestMinSampleSize}";
    }

    if (input.MaturationDays is < 0 or > SearchGoal.MaxMaturationDays)
    {
      errors[nameof(SearchGoalInput.MaturationDays)] = $"Срок созревания — от 0 до {SearchGoal.MaxMaturationDays} дней";
    }

    var currency = string.IsNullOrWhiteSpace(input.IncomeCurrency) ? null : input.IncomeCurrency.Trim();
    if (input.DesiredMonthlyIncome is { } income)
    {
      if (income <= 0)
      {
        errors[nameof(SearchGoalInput.DesiredMonthlyIncome)] = "Доход должен быть больше нуля";
      }

      if (currency is null)
      {
        errors[nameof(SearchGoalInput.IncomeCurrency)] = "Укажите валюту";
      }
    }
    else if (currency is not null)
    {
      errors[nameof(SearchGoalInput.DesiredMonthlyIncome)] = "Укажите желаемый доход";
    }

    if (currency is not null && (currency.Length != 3 || !currency.All(char.IsAsciiLetter)))
    {
      errors[nameof(SearchGoalInput.IncomeCurrency)] = "Валюта — 3 латинские буквы";
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return currency;
  }

  private static SearchGoalDetails ToDetails(SearchGoal goal)
  {
    return new SearchGoalDetails(
      goal.RoleId,
      goal.Locations.Select(l => l.LocationId).ToList(),
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
