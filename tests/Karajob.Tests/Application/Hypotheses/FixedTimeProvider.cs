namespace Karajob.Tests.Application.Hypotheses;

internal class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
  public DateTime UtcNow { get; set; } = utcNow;

  public override DateTimeOffset GetUtcNow()
  {
    return new DateTimeOffset(UtcNow, TimeSpan.Zero);
  }
}
