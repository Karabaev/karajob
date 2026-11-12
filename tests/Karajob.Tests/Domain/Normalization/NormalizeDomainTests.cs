using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeDomainTests
{
  [Theory]
  [InlineData("Ubisoft.com", "ubisoft.com")]
  [InlineData("https://WWW.Ubisoft.com/en-us/", "ubisoft.com")]
  [InlineData("http://ubisoft.com:8080/path?x=1", "ubisoft.com")]
  [InlineData("ubisoft.com.", "ubisoft.com")]
  [InlineData("  careers.ubisoft.com  ", "careers.ubisoft.com")]
  [InlineData("www.ubisoft.com", "ubisoft.com")]
  public void Domain_ValidInput_ReturnsNormalized(string input, string expected)
  {
    Assert.Equal(expected, Normalize.Domain(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("localhost")]
  [InlineData("not a domain")]
  [InlineData("https://")]
  [InlineData("ubi soft.com")]
  public void Domain_InvalidInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.Domain(input));
  }
}
