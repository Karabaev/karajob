using Karajob.Web.Components.Common;

namespace Karajob.Tests.Web.Components.Common;

public class AppTitleTests
{
  [Fact]
  public void Format_WithText_PrefixesAppName()
  {
    Assert.Equal("Karajob | Board", AppTitle.Format("Board"));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Format_NullOrBlank_ReturnsAppNameOnly(string? text)
  {
    Assert.Equal("Karajob", AppTitle.Format(text));
  }
}
