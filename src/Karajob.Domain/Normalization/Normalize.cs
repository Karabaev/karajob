using System.Text;
using System.Text.RegularExpressions;

namespace Karajob.Domain.Normalization;

public static partial class Normalize
{
  private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
  {
    "gclid", "fbclid", "yclid", "mc_cid", "mc_eid", "ref", "refid", "trk", "trkinfo", "trackingid", "lipi", "hhtmfrom", "hhtmsource"
  };

  public static string Slug(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    var slug = value.Trim().ToLowerInvariant();
    if (!SlugPattern().IsMatch(slug)) throw new ArgumentException($"'{value}' is not a valid slug.", nameof(value));
    return slug;
  }

  public static string Domain(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
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

    host = host.TrimEnd('.').ToLowerInvariant();
    if (host.StartsWith("www.", StringComparison.Ordinal))
    {
      host = host[4..];
    }

    if (!host.Contains('.') || Uri.CheckHostName(host) != UriHostNameType.Dns)
    {
      throw new ArgumentException($"'{value}' is not a valid domain.", nameof(value));
    }

    return host;
  }

  public static string Alias(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return WhitespacePattern().Replace(value.Trim(), " ").ToLowerInvariant();
  }

  public static string VacancyUrl(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
      throw new ArgumentException($"'{value}' is not an absolute http(s) URL.", nameof(value));
    }

    var host = uri.Host;
    if (host.StartsWith("www.", StringComparison.Ordinal))
    {
      host = host[4..];
    }

    var result = new StringBuilder("https://").Append(host);
    if (!uri.IsDefaultPort)
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

    return result.ToString().ToLowerInvariant();
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
