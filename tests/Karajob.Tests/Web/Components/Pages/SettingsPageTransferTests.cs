using Bunit;
using Karajob.Web.Transfer;
using Karajob.Tests.Web.Components.Settings;
using SettingsPage = Karajob.Web.Components.Pages.Settings;

namespace Karajob.Tests.Web.Components.Pages;

public class SettingsPageTransferTests : SettingsWebFixture
{
  [Fact]
  public void Render_Always_ShowsExportLinkToTheExportEndpoint()
  {
    var cut = Context.Render<SettingsPage>();

    var link = cut.WaitForElement(".settings-export");

    Assert.Equal(TransferEndpoints.ExportPath, link.GetAttribute("href"));
  }

  [Fact]
  public void Render_Always_ShowsImportButtonAndImportsNothing()
  {
    var cut = Context.Render<SettingsPage>();

    cut.WaitForElement(".settings-import");

    Assert.Equal(0, Transfer.ImportCalls);
  }
}
