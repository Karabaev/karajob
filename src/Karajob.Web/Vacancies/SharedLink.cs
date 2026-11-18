using System.Text.RegularExpressions;

namespace Karajob.Web.Vacancies;

public record SharedLinkResult(string Url, string? Title, string? Note);

public static partial class SharedLink
{
  public static SharedLinkResult Extract(string? url, string? text, string? title)
  {
    var cleanTitle = NullIfBlank(title);
    var trimmedText = text?.Trim();
    var candidate = url?.Trim();
    if (IsHttpUrl(candidate))
    {
      var withoutUrl = trimmedText?.Replace(candidate!, string.Empty, StringComparison.OrdinalIgnoreCase);
      return new SharedLinkResult(candidate!, cleanTitle, NullIfBlank(withoutUrl));
    }

    var match = string.IsNullOrEmpty(trimmedText) ? null : LinkPattern().Match(trimmedText);
    if (match is not { Success: true })
    {
      // A scheme-less value such as "www.x.com/job" is kept in the field so Add can report it instead of losing it.
      var raw = !string.IsNullOrEmpty(candidate) && !Uri.TryCreate(candidate, UriKind.Absolute, out _) ? candidate : string.Empty;
      return new SharedLinkResult(raw, cleanTitle, NullIfBlank(trimmedText));
    }

    var link = TrimTrailingPunctuation(match.Value);
    var rest = trimmedText!.Remove(match.Index, match.Length).Trim();
    return new SharedLinkResult(link, cleanTitle, NullIfBlank(rest));
  }

  private static string TrimTrailingPunctuation(string link)
  {
    var end = link.Length;
    while (end > 0)
    {
      var last = link[end - 1];
      var closer = last is ')' or ']' or '}';
      var balanced = closer && link.AsSpan(0, end).Count(OpeningFor(last)) >= link.AsSpan(0, end).Count(last);
      if (!(".,;:!?>\"'»”’".Contains(last) || (closer && !balanced))) break;

      end--;
    }

    return link[..end];
  }

  private static char OpeningFor(char closer)
  {
    return closer switch { ')' => '(', ']' => '[', _ => '{' };
  }

  private static bool IsHttpUrl(string? value)
  {
    return Uri.TryCreate(value, UriKind.Absolute, out var uri)
      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
  }

  private static string? NullIfBlank(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
  private static partial Regex LinkPattern();
}
