using Karajob.Domain.Common;

namespace Karajob.Domain.Goals;

public class SearchGoal : IUserOwned
{
  public const int MaxConversionThresholdPermille = 1000;
  public const int LowestMinSampleSize = 1;
  public const int MaxMaturationDays = 365;

  private readonly List<SearchGoalLocation> locations = [];
  private readonly List<SearchGoalPlatform> platforms = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid? RoleId { get; set => field = value is { } id ? Require.NotEmpty(id) : null; }
  public IReadOnlyCollection<SearchGoalLocation> Locations => locations;
  public IReadOnlyCollection<SearchGoalPlatform> Platforms => platforms;
  public string? Industries { get; set => field = BlankToNull(value); }
  public string? MustHave { get; set => field = BlankToNull(value); }

  public int WeeklyApplicationTarget
  {
    get;
    set
    {
      ArgumentOutOfRangeException.ThrowIfNegative(value);
      field = value;
    }
  }

  public DateOnly? Deadline { get; set; }
  public long? DesiredMonthlyIncome { get; private set; }
  public string? IncomeCurrency { get; private set; }

  public int MarketThresholdPerWeek
  {
    get;
    set
    {
      ArgumentOutOfRangeException.ThrowIfNegative(value);
      field = value;
    }
  }

  public int ConversionThresholdPermille
  {
    get;
    set
    {
      ArgumentOutOfRangeException.ThrowIfNegative(value);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxConversionThresholdPermille);
      field = value;
    }
  }

  public int MinSampleSize
  {
    get;
    set
    {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, LowestMinSampleSize);
      field = value;
    }
  }

  public int MaturationDays
  {
    get;
    set
    {
      ArgumentOutOfRangeException.ThrowIfNegative(value);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxMaturationDays);
      field = value;
    }
  }

  public void SetLocations(IEnumerable<Guid> locationIds)
  {
    var wanted = locationIds.Select(id => Require.NotEmpty(id, nameof(locationIds))).Distinct().ToList();
    locations.RemoveAll(l => !wanted.Contains(l.LocationId));
    foreach (var locationId in wanted.Where(id => locations.All(l => l.LocationId != id)))
    {
      locations.Add(new SearchGoalLocation(UserId, Id, locationId));
    }
  }

  public void SetPlatforms(IEnumerable<Guid> platformIds)
  {
    throw new NotImplementedException();
  }

  public void SetDesiredIncome(long? amount, string? currency)
  {
    if (amount is null && currency is null)
    {
      DesiredMonthlyIncome = null;
      IncomeCurrency = null;
      return;
    }

    if (amount is null || currency is null) throw new ArgumentException("Amount and currency must be given together.");
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount.Value, 0L, nameof(amount));
    if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetter)) throw new ArgumentException("Currency must be a 3-letter code.", nameof(currency));

    DesiredMonthlyIncome = amount;
    IncomeCurrency = currency.ToUpperInvariant();
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  public SearchGoal(Guid userId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    WeeklyApplicationTarget = SearchGoalDefaults.WeeklyApplicationTarget;
    MarketThresholdPerWeek = SearchGoalDefaults.MarketThresholdPerWeek;
    ConversionThresholdPermille = SearchGoalDefaults.ConversionThresholdPermille;
    MinSampleSize = SearchGoalDefaults.MinSampleSize;
    MaturationDays = SearchGoalDefaults.MaturationDays;
  }
}
