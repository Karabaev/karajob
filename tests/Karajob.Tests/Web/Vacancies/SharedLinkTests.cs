using Karajob.Web.Vacancies;

namespace Karajob.Tests.Web.Vacancies;

public class SharedLinkTests
{
  [Fact]
  public void Extract_UrlOnly_ReturnsUrlWithoutNote()
  {
    var result = SharedLink.Extract("https://x.com/job/1", null, null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Null(result.Note);
  }

  [Fact]
  public void Extract_LinkInTextWithLeadingWords_ExtractsUrlAndKeepsWordsAsNote()
  {
    var result = SharedLink.Extract(null, "Senior Dev at Studio https://x.com/job/1", null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Equal("Senior Dev at Studio", result.Note);
  }

  [Fact]
  public void Extract_LinkInTextWithTrailingWords_ExtractsUrlAndKeepsWordsAsNote()
  {
    var result = SharedLink.Extract(null, "https://x.com/job/1 looks interesting", null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Equal("looks interesting", result.Note);
  }

  [Fact]
  public void Extract_LinkOnlyInText_ReturnsNullNote()
  {
    var result = SharedLink.Extract(null, "  https://x.com/job/1  ", null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Null(result.Note);
  }

  [Fact]
  public void Extract_HttpLinkInText_IsExtracted()
  {
    var result = SharedLink.Extract(null, "see http://x.com/job/1", null);

    Assert.Equal("http://x.com/job/1", result.Url);
    Assert.Equal("see", result.Note);
  }

  [Fact]
  public void Extract_UrlAndText_UrlWinsAndWholeTextBecomesNote()
  {
    var result = SharedLink.Extract("https://x.com/job/1", "Great offer https://y.com/other", null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Equal("Great offer https://y.com/other", result.Note);
  }

  [Theory]
  [InlineData("ftp://x.com/file")]
  [InlineData("javascript:alert(1)")]
  [InlineData("/relative/path")]
  public void Extract_UnsupportedUrlParam_FallsBackToLinkInText(string url)
  {
    var result = SharedLink.Extract(url, "Job https://x.com/job/1", null);

    Assert.Equal("https://x.com/job/1", result.Url);
    Assert.Equal("Job", result.Note);
  }

  [Theory]
  [InlineData("ftp://x.com/file")]
  [InlineData("javascript:alert(1)")]
  public void Extract_UnsupportedUrlParamAndNoLinkInText_ReturnsEmptyUrl(string url)
  {
    var result = SharedLink.Extract(url, "just words", null);

    Assert.Equal("", result.Url);
    Assert.Equal("just words", result.Note);
  }

  [Fact]
  public void Extract_NoLinkAnywhere_ReturnsEmptyUrlAndTrimmedTextAsNote()
  {
    var result = SharedLink.Extract(null, "  just some words  ", null);

    Assert.Equal("", result.Url);
    Assert.Equal("just some words", result.Note);
  }

  [Fact]
  public void Extract_Title_IsPassedThroughTrimmed()
  {
    var result = SharedLink.Extract("https://x.com/job/1", null, "  Senior Dev  ");

    Assert.Equal("Senior Dev", result.Title);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void Extract_BlankTitle_ReturnsNullTitle(string? title)
  {
    var result = SharedLink.Extract("https://x.com/job/1", null, title);

    Assert.Null(result.Title);
  }

  [Fact]
  public void Extract_AllNull_ReturnsEmptyUrlAndNullTitleAndNote()
  {
    var result = SharedLink.Extract(null, null, null);

    Assert.Equal("", result.Url);
    Assert.Null(result.Title);
    Assert.Null(result.Note);
  }

  [Fact]
  public void Extract_BlankText_ReturnsNullNote()
  {
    var result = SharedLink.Extract(null, "   ", null);

    Assert.Equal("", result.Url);
    Assert.Null(result.Note);
  }
}
