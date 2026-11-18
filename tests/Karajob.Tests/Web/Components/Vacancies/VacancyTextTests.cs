using Karajob.Web.Components.Vacancies;

namespace Karajob.Tests.Web.Components.Vacancies;

public class VacancyTextTests
{
  [Theory]
  [InlineData("https://example.com/jobs/1", "https://example.com/jobs/1")]
  [InlineData("http://example.com/jobs/1", "http://example.com/jobs/1")]
  [InlineData("  https://example.com/jobs/1  ", "https://example.com/jobs/1")]
  [InlineData("https://example.com", "https://example.com/")]
  public void OpenableUrl_HttpOrHttpsUrl_ReturnsAbsoluteUri(string url, string expected)
  {
    Assert.Equal(expected, VacancyText.OpenableUrl(url));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not a url")]
  [InlineData("javascript:alert(1)")]
  [InlineData("ftp://x.com")]
  [InlineData("/relative/path")]
  public void OpenableUrl_NotOpenable_ReturnsNull(string? url)
  {
    Assert.Null(VacancyText.OpenableUrl(url));
  }
}
