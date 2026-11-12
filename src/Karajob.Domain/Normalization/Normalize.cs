using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Karajob.Domain.Normalization;

public static partial class Normalize
{
  private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
  {
    "gclid", "fbclid", "yclid", "msclkid", "igshid", "_ga", "_gl", "mc_cid", "mc_eid",
    "ref", "refid", "trk", "trkinfo", "trackingid", "lipi", "alternatechannel", "ebp",
    "gh_src", "lever-origin", "lever-source",
    "hhtmfrom", "hhtmsource",
  };

  public static string Slug(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    var slug = value.Trim().ToLowerInvariant();
    if (!SlugPattern().IsMatch(slug)) throw new ArgumentException($"'{value}' is not a valid slug.", paramName);
    return slug;
  }

  public static string Domain(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    var host = value.Trim();
    var schemeEnd = host.IndexOf("://", StringComparison.Ordinal);
    if (schemeEnd >= 0)
    {
      host = host[(schemeEnd + 3)..];
    }

    var hostEnd = host.IndexOfAny(['/', '?', '#']);
    if (hostEnd >= 0)
    {
      host = host[..hostEnd];
    }

    var portStart = host.LastIndexOf(':');
    if (portStart >= 0)
    {
      host = host[..portStart];
    }

    host = StripWww(host.TrimEnd('.').ToLowerInvariant());
    if (!host.Contains('.') || Uri.CheckHostName(host) != UriHostNameType.Dns)
    {
      throw new ArgumentException($"'{value}' is not a valid domain.", paramName);
    }

    return host;
  }

  public static string Alias(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    return WhitespacePattern().Replace(value.Trim(), " ").ToLowerInvariant();
  }

  public static string WebUrl(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    ParseHttpUrl(value, paramName);
    return value.Trim().ToLowerInvariant();
  }

  public static string VacancyUrl(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    var uri = ParseHttpUrl(value, paramName);
    var result = new StringBuilder("https://").Append(StripWww(uri.Host));
    // The scheme is forced to https, so an explicit :443 on an http URL is the default port too.
    if (!uri.IsDefaultPort && uri.Port != 443)
    {
      result.Append(':').Append(uri.Port);
    }

    result.Append(uri.AbsolutePath.TrimEnd('/'));

    var kept = uri.Query.TrimStart('?')
      .Split('&', StringSplitOptions.RemoveEmptyEntries)
      .Where(p => !IsTrackingParam(ParamName(p)))
      .ToList();
    if (kept.Count > 0)
    {
      result.Append('?').Append(string.Join('&', kept));
    }

    // Hash-routed career sites keep the job id in the fragment; plain anchors like #apply are noise.
    if (uri.Fragment.StartsWith("#/", StringComparison.Ordinal) || uri.Fragment.StartsWith("#!", StringComparison.Ordinal))
    {
      result.Append(uri.Fragment);
    }

    return result.ToString().ToLowerInvariant();
  }

  private static Uri ParseHttpUrl(string value, string? paramName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
      throw new ArgumentException($"'{value}' is not an absolute http(s) URL.", paramName);
    }

    return uri;
  }

  private static string StripWww(string host)
  {
    return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
  }

  private static string ParamName(string param)
  {
    var separator = param.IndexOf('=');
    return separator < 0 ? param : param[..separator];
  }

  private static bool IsTrackingParam(string name)
  {
    return name.StartsWith("utm_", StringComparison.OrdinalIgnoreCase) || TrackingParams.Contains(name);
  }

  [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
  private static partial Regex SlugPattern();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespacePattern();
}
