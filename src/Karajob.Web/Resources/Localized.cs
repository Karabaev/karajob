using System.Globalization;
using System.Resources;

namespace Karajob.Web.Resources;

// For static helpers that have no DI access; components use the injected IStringLocalizer instead.
public static class Localized
{
  public static string Get<TResource>(string key)
  {
    return Holder<TResource>.Manager.GetString(key, CultureInfo.CurrentUICulture)
      ?? throw new KeyNotFoundException($"Resource key '{key}' is missing in {typeof(TResource).Name}.");
  }

  private static class Holder<TResource>
  {
    public static readonly ResourceManager Manager = new(typeof(TResource));
  }
}
