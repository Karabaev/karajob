using Karajob.Web;

namespace Karajob.Tests.Web;

public class AppVersionTests
{
  [Fact]
  public void FromInformationalVersion_FullHash_ReturnsLowercasedFirstSevenChars()
  {
    var version = AppVersion.FromInformationalVersion("1.0.0+A37224FC2A65360F822343E7C0AD1943E6C60924");

    Assert.Equal("a37224f", version);
  }

  [Fact]
  public void FromInformationalVersion_NoPlus_ReturnsDev()
  {
    Assert.Equal("dev", AppVersion.FromInformationalVersion("1.0.0"));
  }

  [Fact]
  public void FromInformationalVersion_Null_ReturnsDev()
  {
    Assert.Equal("dev", AppVersion.FromInformationalVersion(null));
  }

  [Fact]
  public void FromInformationalVersion_EmptyAfterPlus_ReturnsDev()
  {
    Assert.Equal("dev", AppVersion.FromInformationalVersion("1.0.0+"));
  }

  [Fact]
  public void FromInformationalVersion_ShortHash_ReturnsWholeHash()
  {
    Assert.Equal("abc", AppVersion.FromInformationalVersion("1.0.0+abc"));
  }
}
