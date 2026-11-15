namespace Karajob.Web.Components.Settings;

public static class ConversionPercent
{
  public static decimal FromPermille(int permille)
  {
    return permille / 10m;
  }

  public static int ToPermille(decimal percent)
  {
    // Out-of-range input must reach the service as a validation error, not crash the circuit with OverflowException.
    var permille = Math.Round(percent * 10m, MidpointRounding.AwayFromZero);
    return (int)Math.Clamp(permille, int.MinValue, int.MaxValue);
  }
}
