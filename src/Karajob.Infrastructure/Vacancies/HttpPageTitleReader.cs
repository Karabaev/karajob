using System.Net.Http.Headers;
using System.Text;
using Karajob.Application.Vacancies;
using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Vacancies;

public class HttpPageTitleReader(HttpClient http, ILogger<HttpPageTitleReader> logger) : IPageTitleReader
{
  private const int MaxBodyBytes = 512 * 1024;

  public async Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken = default)
  {
    try
    {
      using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

      var html = await ReadBodyAsync(response, contentType!, cancellationToken);
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

  private static async Task<string> ReadBodyAsync(HttpResponseMessage response, MediaTypeHeaderValue contentType, CancellationToken cancellationToken)
  {
    var encoding = contentType.CharSet is { Length: > 0 } charset
      ? Encoding.GetEncoding(charset.Trim('"'))
      : Encoding.UTF8;

    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var buffer = new byte[MaxBodyBytes];
    var total = 0;
    while (total < buffer.Length)
    {
      var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
      if (read == 0)
      {
        break;
      }

      total += read;
    }

    return encoding.GetString(buffer, 0, total);
  }
}
