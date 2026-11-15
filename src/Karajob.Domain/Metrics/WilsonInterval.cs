namespace Karajob.Domain.Metrics;

public static class WilsonInterval
{
  public const double Z95 = 1.959963984540054;

  public static ProportionInterval Compute(int successes, int trials, double z = Z95)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(trials, 1);
    ArgumentOutOfRangeException.ThrowIfNegative(successes);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(successes, trials);

    double n = trials;
    var p = successes / n;
    var z2 = z * z;
    var denominator = 1 + z2 / n;
    var center = (p + z2 / (2 * n)) / denominator;
    var margin = z * Math.Sqrt(p * (1 - p) / n + z2 / (4 * n * n)) / denominator;
    return new ProportionInterval(Math.Max(0, center - margin), Math.Min(1, center + margin));
  }
}
