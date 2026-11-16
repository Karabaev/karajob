using System.Net;
using System.Text.RegularExpressions;

namespace Karajob.Infrastructure.Vacancies;

public static partial class PageTitleParser
{
  public static string? Parse(string html)
  {
    var metas = MetaTagRegex().Matches(html)
      .Select(m => ReadAttributes(m.Value))
      .ToList();

    foreach (var key in new[] { "og:title", "twitter:title" })
    {
      foreach (var attributes in metas)
      {
        var isMatch = (attributes.TryGetValue("property", out var property) && IsKey(property, key))
          || (attributes.TryGetValue("name", out var name) && IsKey(name, key));
        if (!isMatch || !attributes.TryGetValue("content", out var content)) continue;

        var normalized = Normalize(content);
        if (normalized is not null)
        {
          return normalized;
        }
      }
    }

    var titleMatch = TitleRegex().Match(html);
    return titleMatch.Success ? Normalize(titleMatch.Groups[1].Value) : null;
  }

  private static bool IsKey(string value, string key)
  {
    return string.Equals(value.Trim(), key, StringComparison.OrdinalIgnoreCase);
  }

  private static Dictionary<string, string> ReadAttributes(string tag)
  {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (Match m in AttributeRegex().Matches(tag))
    {
      var value = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
      result.TryAdd(m.Groups[1].Value, value);
    }

    return result;
  }

  private static string? Normalize(string raw)
  {
    var decoded = WebUtility.HtmlDecode(raw);
    var collapsed = WhitespaceRegex().Replace(decoded, " ").Trim();
    return collapsed.Length == 0 ? null : collapsed;
  }

  [GeneratedRegex(@"<meta\b(?:[^>""']|""[^""]*""|'[^']*')*>", RegexOptions.IgnoreCase)]
  private static partial Regex MetaTagRegex();

  [GeneratedRegex(@"([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+))", RegexOptions.IgnoreCase)]
  private static partial Regex AttributeRegex();

  [GeneratedRegex("<title\\b[^>]*>(.*?)</title\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
  private static partial Regex TitleRegex();

  [GeneratedRegex("\\s+")]
  private static partial Regex WhitespaceRegex();
}
