using System.Text.RegularExpressions;

namespace Karajob.Tests.Localization;

public partial class NoCyrillicInSourceTests
{
  private static readonly string[] AllowedFiles = ["Karajob.Domain/Normalization/Normalize.cs"];

  [Fact]
  public void SourceFiles_ContainNoCyrillicOutsideAllowList()
  {
    var offenders = new List<string>();
    foreach (var file in SourceFiles())
    {
      var lines = File.ReadAllLines(file);
      for (var i = 0; i < lines.Length; i++)
      {
        if (CyrillicPattern().IsMatch(lines[i]))
        {
          offenders.Add($"{Relative(file)}:{i + 1}");
        }
      }
    }

    Assert.True(offenders.Count == 0, $"Cyrillic text found in {offenders.Count} places:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
  }

  [Fact]
  public void SourceScan_FindsProjectFiles()
  {
    var files = SourceFiles().Select(Relative).ToList();

    Assert.Contains(files, f => f.EndsWith("Karajob.Web/Components/Pages/Inbox.razor", StringComparison.Ordinal));
    Assert.Contains(files, f => f.EndsWith("Karajob.Application/Vacancies/VacancyService.cs", StringComparison.Ordinal));
  }

  private static IEnumerable<string> SourceFiles()
  {
    return Directory.EnumerateFiles(RepositoryPaths.Src, "*.*", SearchOption.AllDirectories)
      .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal))
      .Where(f => !Segments(f).Any(s => s is "bin" or "obj" or "Migrations"))
      .Where(f => !AllowedFiles.Contains(Relative(f)));
  }

  private static string Relative(string path)
  {
    return Path.GetRelativePath(RepositoryPaths.Src, path).Replace(Path.DirectorySeparatorChar, '/');
  }

  private static string[] Segments(string path)
  {
    return Path.GetRelativePath(RepositoryPaths.Src, path).Split(Path.DirectorySeparatorChar, '/');
  }

  [GeneratedRegex("[Ѐ-ӿ]")]
  private static partial Regex CyrillicPattern();
}
