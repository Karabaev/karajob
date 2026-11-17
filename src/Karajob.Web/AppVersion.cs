using System.Reflection;

namespace Karajob.Web;

public static class AppVersion
{
  public static string Current { get; } =
    FromInformationalVersion(typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

  public static string FromInformationalVersion(string? informationalVersion)
  {
    throw new NotImplementedException();
  }
}
