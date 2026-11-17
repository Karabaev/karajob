using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Karajob.Tests.Localization;

public partial class ResourceFilesTests
{
  private static readonly string[] ApplicationMarkers =
    ["VacancyMessages", "CvMessages", "CompanyMessages", "TrackMessages", "GoalMessages", "CatalogMessages", "UserMessages"];

  private static readonly string[] WebMarkers =
    ["LayoutStrings", "PagesStrings", "VacancyStrings", "CompanyStrings", "TrackStrings", "CvStrings", "CatalogStrings", "ProfileStrings"];

  public static TheoryData<string> Markers
  {
    get
    {
      var data = new TheoryData<string>();
      foreach (var marker in ApplicationMarkers.Concat(WebMarkers))
      {
        data.Add(marker);
      }

      return data;
    }
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void NeutralResx_Exists_HasEntries(string marker)
  {
    var entries = Read(NeutralPath(marker));

    Assert.NotEmpty(entries);
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void RussianResx_HasSameKeysAsNeutral(string marker)
  {
    var neutral = Read(NeutralPath(marker));
    var russian = Read(RussianPath(marker));

    Assert.Equal(neutral.Keys.Order(StringComparer.Ordinal), russian.Keys.Order(StringComparer.Ordinal));
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void Resx_AllValuesAreNonEmpty(string marker)
  {
    var empty = Read(NeutralPath(marker)).Concat(Read(RussianPath(marker)))
      .Where(e => string.IsNullOrWhiteSpace(e.Value))
      .Select(e => e.Key)
      .Distinct()
      .ToList();

    Assert.Empty(empty);
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void NeutralResx_ValuesContainNoCyrillic(string marker)
  {
    var offenders = Read(NeutralPath(marker)).Where(e => CyrillicPattern().IsMatch(e.Value)).Select(e => e.Key).ToList();

    Assert.Empty(offenders);
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void RussianResx_ValuesAreRussianUnlessIdenticalToNeutral(string marker)
  {
    var neutral = Read(NeutralPath(marker));
    var russian = Read(RussianPath(marker));

    var untranslated = russian
      .Where(e => neutral.TryGetValue(e.Key, out var english) && e.Value != english && !CyrillicPattern().IsMatch(e.Value))
      .Select(e => e.Key)
      .ToList();

    Assert.Empty(untranslated);
  }

  [Theory]
  [MemberData(nameof(Markers))]
  public void RussianResx_KeepsFormatPlaceholdersOfNeutral(string marker)
  {
    var neutral = Read(NeutralPath(marker));
    var russian = Read(RussianPath(marker));

    var mismatched = neutral
      .Where(e => russian.TryGetValue(e.Key, out var value) && Placeholders(value).SequenceEqual(Placeholders(e.Value)) == false)
      .Select(e => e.Key)
      .ToList();

    Assert.Empty(mismatched);
  }

  [Fact]
  public void Resx_RussianTranslationsDifferFromNeutralForMostEntries()
  {
    var total = 0;
    var identical = 0;
    foreach (var marker in ApplicationMarkers.Concat(WebMarkers))
    {
      var neutral = Read(NeutralPath(marker));
      var russian = Read(RussianPath(marker));
      total += neutral.Count;
      identical += neutral.Count(e => russian.TryGetValue(e.Key, out var value) && value == e.Value);
    }

    Assert.True(total > 0, "Resource files are empty.");
    Assert.True(identical * 5 < total, $"{identical} of {total} Russian entries are identical to the English ones.");
  }

  private static string NeutralPath(string marker)
  {
    return Path.Combine(ResourcesDirectory(marker), marker + ".resx");
  }

  private static string RussianPath(string marker)
  {
    return Path.Combine(ResourcesDirectory(marker), marker + ".ru.resx");
  }

  private static string ResourcesDirectory(string marker)
  {
    var project = ApplicationMarkers.Contains(marker) ? "Karajob.Application" : "Karajob.Web";
    return Path.Combine(RepositoryPaths.Src, project, "Resources");
  }

  private static Dictionary<string, string> Read(string path)
  {
    Assert.True(File.Exists(path), $"Resource file '{path}' does not exist.");
    var document = XDocument.Load(path);
    return document.Root!.Elements("data")
      .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element("value")?.Value ?? string.Empty);
  }

  private static IEnumerable<string> Placeholders(string value)
  {
    return PlaceholderPattern().Matches(value).Select(m => m.Value).Order(StringComparer.Ordinal);
  }

  [GeneratedRegex("[Ѐ-ӿ]")]
  private static partial Regex CyrillicPattern();

  [GeneratedRegex(@"\{\d+\}")]
  private static partial Regex PlaceholderPattern();
}
