namespace Karajob.Tests.Localization;

internal static class RepositoryPaths
{
  public static string Root { get; } = FindRoot();

  public static string Src => Path.Combine(Root, "src");

  private static string FindRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "Karajob.slnx")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException($"Karajob.slnx was not found above '{AppContext.BaseDirectory}'.");
  }
}
