using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Karajob.Application.Vacancies;
using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Vacancies;

public partial class HttpPageTitleReader(HttpClient http, ILogger<HttpPageTitleReader> logger) : IPageTitleReader
{
  private const int MaxBodyBytes = 512 * 1024;
  private const int CharsetSniffBytes = 2048;

  public async Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken = default)
  {
    // HttpClient.Timeout stops at the response headers (ResponseHeadersRead), so the same budget is applied to the body.
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    if (http.Timeout != Timeout.InfiniteTimeSpan)
    {
      timeout.CancelAfter(http.Timeout);
    }

    try
    {
      using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
      if (!response.IsSuccessStatusCode)
      {
        logger.LogInformation("Page title not read from {Url}: status {Status}", url, (int)response.StatusCode);
        return null;
      }

      var contentType = response.Content.Headers.ContentType;
      if (!IsHtml(contentType))
      {
        logger.LogInformation("Page title not read from {Url}: media type {MediaType}", url, contentType?.MediaType);
        return null;
      }

      var body = await ReadBodyAsync(response, timeout.Token);
      var html = DetectEncoding(contentType!, body).GetString(body);
      return PageTitleParser.Parse(html);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      logger.LogInformation("Page title not read from {Url}: request timed out", url);
      return null;
    }
    catch (Exception ex) when (ex is HttpRequestException or ArgumentException or DecoderFallbackException or IOException)
    {
      logger.LogInformation("Page title not read from {Url}: {Reason}", url, ex.Message);
      return null;
    }
  }

  private static bool IsHtml(MediaTypeHeaderValue? contentType)
  {
    return contentType?.MediaType is "text/html" or "application/xhtml+xml";
  }

  private static async Task<byte[]> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var buffer = new byte[MaxBodyBytes];
    var total = 0;
    while (total < buffer.Length)
    {
      var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
      if (read == 0) break;

      total += read;
    }

    return buffer[..total];
  }

  private static Encoding DetectEncoding(MediaTypeHeaderValue contentType, byte[] body)
  {
    if (contentType.CharSet is { Length: > 0 } charset)
    {
      return Encoding.GetEncoding(charset.Trim('"'));
    }

    var bomEncoding = BomEncoding(body);
    if (bomEncoding is not null)
    {
      return bomEncoding;
    }

    var head = Encoding.Latin1.GetString(body, 0, Math.Min(body.Length, CharsetSniffBytes));
    var meta = MetaCharsetRegex().Match(head);
    if (!meta.Success) return Encoding.UTF8;

    try
    {
      return Encoding.GetEncoding(meta.Groups[1].Value);
    }
    catch (ArgumentException)
    {
      return Encoding.UTF8;
    }
  }

  private static Encoding? BomEncoding(byte[] body)
  {
    return body switch
    {
      [0xEF, 0xBB, 0xBF, ..] => Encoding.UTF8,
      [0xFF, 0xFE, ..] => Encoding.Unicode,
      [0xFE, 0xFF, ..] => Encoding.BigEndianUnicode,
      _ => null,
    };
  }

  [GeneratedRegex(@"<meta\b[^>]*?charset\s*=\s*[""']?([-\w:.]+)", RegexOptions.IgnoreCase)]
  private static partial Regex MetaCharsetRegex();

  // Pages declare legacy charsets such as windows-1251, which .NET resolves only through the code pages provider.
  static HttpPageTitleReader()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
  }
}
