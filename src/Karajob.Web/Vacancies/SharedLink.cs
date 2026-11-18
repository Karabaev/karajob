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
      return new SharedLinkResult(candidate!, cleanTitle, NullIfBlank(trimmedText));
    }

    var match = string.IsNullOrEmpty(trimmedText) ? null : LinkPattern().Match(trimmedText);
    if (match is not { Success: true })
    {
      return new SharedLinkResult(string.Empty, cleanTitle, NullIfBlank(trimmedText));
    }

    var rest = trimmedText!.Remove(match.Index, match.Length).Trim();
    return new SharedLinkResult(match.Value, cleanTitle, NullIfBlank(rest));
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
