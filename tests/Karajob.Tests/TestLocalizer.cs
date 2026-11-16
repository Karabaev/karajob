using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Karajob.Tests;

internal static class TestLocalizer
{
  private static readonly ResourceManagerStringLocalizerFactory Factory =
    new(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);

  public static IStringLocalizer<T> For<T>()
  {
    return new StringLocalizer<T>(Factory);
  }
}
