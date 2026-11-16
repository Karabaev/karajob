using Karajob.Application.Resources;
using Karajob.Domain.Normalization;

namespace Karajob.Tests.Application.MasterProfiles;

internal static class MasterProfileTestTemplate
{
  public static string English => In(TestCulture.English);

  public static string In(System.Globalization.CultureInfo culture)
  {
    using var scope = TestCulture.Use(culture);
    return Normalize.LineEndings(TestLocalizer.For<MasterProfileMessages>()["Template"]);
  }
}
