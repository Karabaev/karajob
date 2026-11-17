using System.Reflection;

namespace Karajob.Web;

public static class AppVersion
{
  private const int ShortHashLength = 7;
  private const string Unknown = "dev";

  public static string Current { get; } =
    FromInformationalVersion(typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

  public static string FromInformationalVersion(string? informationalVersion)
  {
    var plus = informationalVersion?.IndexOf('+') ?? -1;
    if (plus < 0) return Unknown;

    var hash = informationalVersion![(plus + 1)..];
    if (hash.Length == 0) return Unknown;

    return hash[..Math.Min(ShortHashLength, hash.Length)].ToLowerInvariant();
  }
}
