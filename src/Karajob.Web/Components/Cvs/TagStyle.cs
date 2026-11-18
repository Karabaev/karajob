using System.Globalization;

namespace Karajob.Web.Components.Cvs;

public static class TagStyle
{
  public static string? ChipStyle(string? color)
  {
    return color is null ? null : $"background-color:{color};color:{ContrastText(color)};";
  }

  private static string ContrastText(string color)
  {
    var red = int.Parse(color.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var green = int.Parse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var blue = int.Parse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var luminance = (299 * red + 587 * green + 114 * blue) / 1000;
    return luminance >= 140 ? "#000000" : "#ffffff";
  }
}
