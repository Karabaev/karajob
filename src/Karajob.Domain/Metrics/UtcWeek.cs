namespace Karajob.Domain.Metrics;

public readonly record struct UtcWeek(DateTime Start, DateTime End)
{
  public static UtcWeek Containing(DateTime utc)
  {
    throw new NotImplementedException();
  }
}
