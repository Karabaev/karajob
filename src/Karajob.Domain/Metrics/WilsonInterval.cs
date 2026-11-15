namespace Karajob.Domain.Metrics;

public static class WilsonInterval
{
  public const double Z95 = 1.959963984540054;

  public static ProportionInterval Compute(int successes, int trials, double z = Z95)
  {
    throw new NotImplementedException();
  }
}
