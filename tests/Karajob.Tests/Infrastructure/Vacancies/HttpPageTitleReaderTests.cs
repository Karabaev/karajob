using System.Net;
using System.Text;
using Karajob.Infrastructure.Vacancies;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karajob.Tests.Infrastructure.Vacancies;

public class HttpPageTitleReaderTests
{
  private const string Url = "https://example.com/jobs/1";

  [Fact]
  public async Task ReadTitleAsync_HtmlWithOgTitle_ReturnsTitle()
  {
    var reader = ReaderFor(Html("<meta property=\"og:title\" content=\"Unity Dev\">"));

    Assert.Equal("Unity Dev", await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_NotFound_ReturnsNull()
  {
    var reader = ReaderFor(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
    {
      Content = new StringContent("<title>Nope</title>", Encoding.UTF8, "text/html")
    });

    Assert.Null(await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_JsonContent_ReturnsNull()
  {
    var reader = ReaderFor(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("{\"title\":\"x\"}", Encoding.UTF8, "application/json")
    });

    Assert.Null(await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_HandlerThrowsHttpRequestException_ReturnsNull()
  {
    var reader = ReaderFor(_ => throw new HttpRequestException("boom"));

    Assert.Null(await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_TimeoutWithoutCallerCancellation_ReturnsNull()
  {
    var reader = ReaderFor(_ => throw new TaskCanceledException("timeout"));

    Assert.Null(await reader.ReadTitleAsync(Url, CancellationToken.None));
  }

  [Fact]
  public async Task ReadTitleAsync_CallerTokenCancelled_ThrowsOperationCanceled()
  {
    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();
    var reader = ReaderFor(Html("<title>x</title>"));

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadTitleAsync(Url, cts.Token));
  }

  [Fact]
  public async Task ReadTitleAsync_LargeBodyWithTitleAtStart_ReturnsTitle()
  {
    var body = "<html><head><title>Early</title></head><body>" + new string('x', 600 * 1024) + "</body></html>";
    var reader = ReaderFor(Html(body));

    Assert.Equal("Early", await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_TitleAfterBodyCap_ReturnsNull()
  {
    var body = "<html><body>" + new string('x', 600 * 1024) + "<title>Late</title></body></html>";
    var reader = ReaderFor(Html(body));

    Assert.Null(await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_BodyStallsAfterHeaders_ReturnsNullWithinTimeout()
  {
    var http = new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StreamContent(new StallingStream()) { Headers = { { "Content-Type", "text/html" } } }
    }))
    {
      Timeout = TimeSpan.FromMilliseconds(200)
    };
    var reader = new HttpPageTitleReader(http, NullLogger<HttpPageTitleReader>.Instance);

    var read = reader.ReadTitleAsync(Url);
    var finished = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(10)));

    Assert.Same(read, finished);
    Assert.Null(await read);
  }

  [Fact]
  public async Task ReadTitleAsync_HeaderCharsetWindows1251_DecodesTitle()
  {
    var reader = ReaderFor(Bytes("<title>Разработчик Unity</title>", "windows-1251", "text/html; charset=windows-1251"));

    Assert.Equal("Разработчик Unity", await reader.ReadTitleAsync(Url));
  }

  [Fact]
  public async Task ReadTitleAsync_MetaCharsetOnly_DecodesTitle()
  {
    var html = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=windows-1251\"><title>Разработчик</title>";
    var reader = ReaderFor(Bytes(html, "windows-1251", "text/html"));

    Assert.Equal("Разработчик", await reader.ReadTitleAsync(Url));
  }

  private static Func<HttpRequestMessage, HttpResponseMessage> Bytes(string body, string encoding, string contentType)
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var bytes = Encoding.GetEncoding(encoding).GetBytes(body);
    return _ =>
    {
      var content = new ByteArrayContent(bytes);
      content.Headers.TryAddWithoutValidation("Content-Type", contentType);
      return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    };
  }

  private class StallingStream : MemoryStream
  {
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);
      return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
      return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }
  }

  private static Func<HttpRequestMessage, HttpResponseMessage> Html(string body)
  {
    return _ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(body, Encoding.UTF8, "text/html")
    };
  }

  private static HttpPageTitleReader ReaderFor(Func<HttpRequestMessage, HttpResponseMessage> respond)
  {
    return new HttpPageTitleReader(new HttpClient(new FakeHandler(respond)), NullLogger<HttpPageTitleReader>.Instance);
  }

  private class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(respond(request));
    }
  }
}
