using Karajob.Web.Components.MasterProfiles;

namespace Karajob.Tests.Web.Components.MasterProfiles;

public class MarkdownHtmlTests
{
  private static string Html(string markdown) => MarkdownHtml.Render(markdown).Value;

  [Fact]
  public void Render_Heading_ProducesH2()
  {
    Assert.Contains("<h2", Html("## Title"));
  }

  [Fact]
  public void Render_PipeTable_ProducesTable()
  {
    const string markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

    Assert.Contains("<table", Html(markdown));
  }

  [Fact]
  public void Render_ScriptTag_IsEscaped()
  {
    var html = Html("<script>alert(1)</script>");

    Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("&lt;script&gt;", html);
  }

  [Theory]
  [InlineData("[x](javascript:alert(1))")]
  [InlineData("[x](JavaScript:alert(1))")]
  [InlineData("![i](javascript:alert(1))")]
  [InlineData("<javascript:alert(1)>")]
  public void Render_JavascriptUrl_IsNotEmitted(string markdown)
  {
    Assert.DoesNotContain("javascript:", Html(markdown), StringComparison.OrdinalIgnoreCase);
  }

  [Theory]
  [InlineData("[x][r]\n\n[r]: javascript:alert(1)", "javascript")]
  [InlineData("[x](javascript&#58;alert(1))", "javascript")]
  [InlineData("[x](java&#9;script:alert(1))", "script:")]
  [InlineData("[x](vbscript:msgbox(1))", "vbscript")]
  [InlineData("![i](data:text/html;base64,PHNjcmlwdD4=)", "data:")]
  public void Render_UnsafeUrlVariant_IsNotEmitted(string markdown, string forbidden)
  {
    Assert.DoesNotContain(forbidden, Html(markdown), StringComparison.OrdinalIgnoreCase);
  }

  [Theory]
  [InlineData("see https://example.com/a", "href=\"https://example.com/a\"")]
  [InlineData("see www.example.com", "href=\"http://www.example.com\"")]
  public void Render_BareAutolink_KeepsHref(string markdown, string expected)
  {
    Assert.Contains(expected, Html(markdown));
  }

  [Fact]
  public void Render_HttpsLink_KeepsHref()
  {
    Assert.Contains("href=\"https://example.com\"", Html("[x](https://example.com)"));
  }

  [Fact]
  public void Render_MailtoLink_KeepsHref()
  {
    Assert.Contains("href=\"mailto:me@example.com\"", Html("[x](mailto:me@example.com)"));
  }

  [Fact]
  public void Render_RelativeLink_KeepsHref()
  {
    Assert.Contains("href=\"/vacancies\"", Html("[x](/vacancies)"));
  }
}
