namespace Karajob.Web.Components.Settings;

public static class ConversionPercent
{
  public static decimal FromPermille(int permille)
  {
    return permille / 10m;
  }

  public static int ToPermille(decimal percent)
  {
    return (int)Math.Round(percent * 10m, MidpointRounding.AwayFromZero);
  }
}
