using Karajob.Infrastructure.Vacancies;

namespace Karajob.Tests.Infrastructure.Vacancies;

public class PageTitleParserTests
{
  [Fact]
  public void Parse_OgTitleAndTitle_PrefersOgTitle()
  {
    var html = "<head><title>Plain</title><meta property=\"og:title\" content=\"Open Graph\"></head>";

    Assert.Equal("Open Graph", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_ContentBeforeProperty_ReadsOgTitle()
  {
    var html = "<meta content=\"Reversed\" property=\"og:title\">";

    Assert.Equal("Reversed", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_UppercaseTagsAndAttributes_ReadsOgTitle()
  {
    var html = "<HEAD><META PROPERTY=\"og:title\" CONTENT=\"Loud\"></HEAD>";

    Assert.Equal("Loud", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_SingleQuotedAttributes_ReadsOgTitle()
  {
    var html = "<meta property='og:title' content='Single'>";

    Assert.Equal("Single", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_UppercaseTitleTag_ReadsTitle()
  {
    Assert.Equal("Shouting", PageTitleParser.Parse("<TITLE>Shouting</TITLE>"));
  }

  [Fact]
  public void Parse_TwitterTitleWithoutOgTitle_UsesTwitterTitle()
  {
    var html = "<title>Plain</title><meta name=\"twitter:title\" content=\"Tweet\">";

    Assert.Equal("Tweet", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_OnlyTitle_UsesTitle()
  {
    Assert.Equal("Just title", PageTitleParser.Parse("<html><head><title>Just title</title></head></html>"));
  }

  [Fact]
  public void Parse_HtmlEntities_AreDecoded()
  {
    var html = "<title>Dev &amp; Ops &#8211; &quot;Remote&quot;</title>";

    Assert.Equal("Dev & Ops – \"Remote\"", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_EntitiesInOgTitleContent_AreDecoded()
  {
    var html = "<meta property=\"og:title\" content=\"A &amp; B\">";

    Assert.Equal("A & B", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_WhitespaceAndNewlines_AreCollapsedAndTrimmed()
  {
    var html = "<title>\n   Senior \r\n  Unity\t\tDev  \n</title>";

    Assert.Equal("Senior Unity Dev", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_EmptyOgTitle_FallsBackToTitle()
  {
    var html = "<title>Fallback</title><meta property=\"og:title\" content=\"\">";

    Assert.Equal("Fallback", PageTitleParser.Parse(html));
  }

  [Fact]
  public void Parse_NoTitleAnywhere_ReturnsNull()
  {
    Assert.Null(PageTitleParser.Parse("<html><body><p>hello</p></body></html>"));
  }

  [Fact]
  public void Parse_EmptyString_ReturnsNull()
  {
    Assert.Null(PageTitleParser.Parse(""));
  }
}
