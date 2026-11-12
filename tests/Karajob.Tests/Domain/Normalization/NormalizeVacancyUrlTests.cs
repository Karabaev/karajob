using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeVacancyUrlTests
{
  [Theory]
  [InlineData("https://Jobs.Lever.co/Ubisoft/ABC-123", "https://jobs.lever.co/ubisoft/abc-123")]
  [InlineData("http://www.example.com/jobs/1/", "https://example.com/jobs/1")]
  [InlineData("https://example.com:443/jobs/1", "https://example.com/jobs/1")]
  [InlineData("https://example.com/jobs/1#apply", "https://example.com/jobs/1")]
  [InlineData("https://example.com/jobs?id=5&utm_source=x&UTM_Medium=y&gclid=1&fbclid=2", "https://example.com/jobs?id=5")]
  [InlineData("https://www.linkedin.com/jobs/view/4012345678/?refId=abc&trackingId=xyz&trk=public_jobs", "https://linkedin.com/jobs/view/4012345678")]
  [InlineData("https://hh.ru/vacancy/123?hhtmFrom=vacancy_search_list&hhtmSource=x", "https://hh.ru/vacancy/123")]
  [InlineData("https://example.com/j?b=2&utm_x=1&a=1", "https://example.com/j?b=2&a=1")]
  [InlineData("https://example.com/j?yclid=1&mc_cid=2&mc_eid=3&ref=4&trkInfo=5&lipi=6", "https://example.com/j")]
  [InlineData("https://Example.com/", "https://example.com")]
  [InlineData("https://example.com:8443/jobs", "https://example.com:8443/jobs")]
  public void VacancyUrl_ValidInput_ReturnsNormalized(string input, string expected)
  {
    Assert.Equal(expected, Normalize.VacancyUrl(input));
  }

  [Theory]
  [InlineData("https://Jobs.Lever.co/Ubisoft/ABC-123")]
  [InlineData("http://www.example.com/jobs/1/")]
  [InlineData("https://example.com/jobs?id=5&utm_source=x&gclid=1")]
  [InlineData("https://www.linkedin.com/jobs/view/4012345678/?refId=abc&trk=public_jobs")]
  [InlineData("https://example.com/j?b=2&utm_x=1&a=1")]
  [InlineData("https://Example.com/")]
  [InlineData("https://example.com:8443/jobs")]
  public void VacancyUrl_AppliedTwice_IsIdempotent(string input)
  {
    var once = Normalize.VacancyUrl(input);

    Assert.Equal(once, Normalize.VacancyUrl(once));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not a url")]
  [InlineData("/jobs/1")]
  [InlineData("ftp://example.com/x")]
  [InlineData("mailto:hr@example.com")]
  public void VacancyUrl_InvalidInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.VacancyUrl(input));
  }
}
