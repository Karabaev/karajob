using Karajob.Domain.Common;

namespace Karajob.Domain.Goals;

public class SearchGoal : IUserOwned
{
  private readonly List<SearchGoalLocation> locations = [];

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid? RoleId { get; set; }
  public IReadOnlyCollection<SearchGoalLocation> Locations => locations;
  public string? Industries { get; set; }
  public string? MustHave { get; set; }
  public int WeeklyApplicationTarget { get; set; }
  public DateOnly? Deadline { get; set; }
  public long? DesiredMonthlyIncome { get; private set; }
  public string? IncomeCurrency { get; private set; }
  public int MarketThresholdPerWeek { get; set; }
  public int ConversionThresholdPermille { get; set; }
  public int MinSampleSize { get; set; }
  public int MaturationDays { get; set; }

  public void SetLocations(IEnumerable<Guid> locationIds)
  {
    throw new NotImplementedException();
  }

  public void SetDesiredIncome(long? amount, string? currency)
  {
    throw new NotImplementedException();
  }

  public SearchGoal(Guid userId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
  }
}
