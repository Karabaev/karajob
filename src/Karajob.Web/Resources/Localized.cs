using System.Globalization;
using System.Resources;

namespace Karajob.Web.Resources;

// For static helpers that have no DI access; components use the injected IStringLocalizer instead.
public static class Localized
{
  public static string Get<TResource>(string key)
  {
    return new ResourceManager(typeof(TResource)).GetString(key, CultureInfo.CurrentUICulture) ?? key;
  }
}
