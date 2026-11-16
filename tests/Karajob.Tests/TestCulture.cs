using System.Globalization;
using System.Runtime.CompilerServices;

namespace Karajob.Tests;

internal static class TestCulture
{
  public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
  public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");

  // The machine's own UI culture must not decide which satellite resx a test reads.
  [ModuleInitializer]
  internal static void UseEnglishByDefault()
  {
    CultureInfo.DefaultThreadCurrentUICulture = English;
    CultureInfo.CurrentUICulture = English;
  }

  public static IDisposable Use(CultureInfo culture)
  {
    return new Scope(culture);
  }

  private sealed class Scope : IDisposable
  {
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    public Scope(CultureInfo culture)
    {
      CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
      CultureInfo.CurrentUICulture = _previous;
    }
  }
}
