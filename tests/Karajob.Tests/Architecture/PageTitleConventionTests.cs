using Karajob.Tests.Localization;

namespace Karajob.Tests.Architecture;

public class PageTitleConventionTests
{
  private const string AllowedFile = "Karajob.Web/Components/Common/AppTitle.razor";

  [Fact]
  public void RazorFiles_UseAppTitleInsteadOfPageTitle()
  {
    var offenders = RazorFiles()
      .Where(f => !Relative(f).Equals(AllowedFile, StringComparison.Ordinal))
      .Where(f => File.ReadAllText(f).Contains("<PageTitle", StringComparison.Ordinal))
      .Select(Relative)
      .ToList();

    Assert.True(offenders.Count == 0, $"<PageTitle> used directly in {offenders.Count} files, use <AppTitle>:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
  }

  [Fact]
  public void RazorScan_FindsBoardPage()
  {
    var files = RazorFiles().Select(Relative).ToList();

    Assert.Contains("Karajob.Web/Components/Pages/Board.razor", files);
  }

  private static IEnumerable<string> RazorFiles()
  {
    return Directory.EnumerateFiles(RepositoryPaths.Src, "*.razor", SearchOption.AllDirectories)
      .Where(f => !Path.GetRelativePath(RepositoryPaths.Src, f).Split(Path.DirectorySeparatorChar, '/').Any(s => s is "bin" or "obj"));
  }

  private static string Relative(string path)
  {
    return Path.GetRelativePath(RepositoryPaths.Src, path).Replace(Path.DirectorySeparatorChar, '/');
  }
}
