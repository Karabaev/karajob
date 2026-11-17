using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Cvs;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Cvs;

public class CvPageTests : CvWebFixture
{
  private IRenderedComponent<MudDialogProvider> dialogs = null!;

  private IRenderedComponent<CvPage> RenderPage(Guid id)
  {
    Context.Render<MudPopoverProvider>();
    dialogs = Context.Render<MudDialogProvider>();
    return Context.Render<CvPage>(p => p.Add(c => c.Id, id));
  }

  [Fact]
  public async Task Render_ExistingCvWithTwoVersions_ShowsBothFileNames()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("first.pdf"));
    await Service.AddVersionAsync(created.Id, Upload("second.pdf"));

    var cut = Context.Render<CvPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("first.pdf", cut.Markup);
      Assert.Contains("second.pdf", cut.Markup);
    });
  }

  [Fact]
  public async Task Render_ExistingCvWithTwoVersions_LinksEachVersionToDownloadEndpoint()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("first.pdf"));
    var details = await Service.AddVersionAsync(created.Id, Upload("second.pdf"));

    var cut = Context.Render<CvPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() =>
    {
      var links = cut.FindAll("a[download]");
      foreach (var version in details.Versions)
      {
        Assert.Contains(
          links,
          a => a.GetAttribute("href")!.EndsWith($"/cvs/versions/{version.Id}/file", StringComparison.OrdinalIgnoreCase));
      }
    });
  }

  [Fact]
  public async Task Render_PdfVersion_ShowsPreviewControl()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("only.pdf"));

    var cut = RenderPage(created.Id);

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".cv-preview")));
  }

  [Fact]
  public async Task Render_DocxVersion_HasNoPreviewControlButKeepsDownload()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("only.docx"));

    var cut = RenderPage(created.Id);

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll("a[download]")));
    Assert.Empty(cut.FindAll(".cv-preview"));
  }

  [Fact]
  public async Task Render_PdfAndDocxVersions_PreviewOnlyForPdfAndDownloadForBoth()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("one.pdf"));
    await Service.AddVersionAsync(created.Id, Upload("two.docx"));

    var cut = RenderPage(created.Id);

    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("a[download]").Count));
    Assert.Single(cut.FindAll(".cv-preview"));
  }

  [Fact]
  public async Task Preview_Clicked_OpensDialogWithInlineFrame()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("only.pdf"));
    var versionId = created.Versions[0].Id;
    var cut = RenderPage(created.Id);

    cut.WaitForElement(".cv-preview").Click();

    var frame = dialogs.WaitForElement("iframe.cv-preview-frame");
    Assert.Equal($"/cvs/versions/{versionId}/file?inline=true", frame.GetAttribute("src"));
  }

  [Fact]
  public async Task Preview_Clicked_DialogHasOpenInNewTabLink()
  {
    var created = await Service.CreateAsync(Input("Main"), Upload("only.pdf"));
    var versionId = created.Versions[0].Id;
    var cut = RenderPage(created.Id);

    cut.WaitForElement(".cv-preview").Click();

    var link = dialogs.WaitForElement("a.cv-preview-open");
    Assert.Equal($"/cvs/versions/{versionId}/file?inline=true", link.GetAttribute("href"));
    Assert.Equal("_blank", link.GetAttribute("target"));
  }

  [Fact]
  public void Render_UnknownId_ShowsNotFound()
  {
    var cut = Context.Render<CvPage>(p => p.Add(c => c.Id, Guid.NewGuid()));

    cut.WaitForAssertion(() => Assert.Contains("CV not found", cut.Markup));
  }

  [Fact]
  public async Task Save_ValidNewCv_PersistsDocumentWithUploadedVersion()
  {
    var cut = Context.Render<CvPage>();
    cut.WaitForElement(".cv-name input").Input("Fresh CV");
    cut.WaitForElement(".cv-language");
    var language = cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class!.Contains("cv-language"));
    await cut.InvokeAsync(() => language.Instance.ValueChanged.InvokeAsync(SeedData.EnglishLanguageId));
    var upload = cut.FindComponent<InputFile>();
    await cut.InvokeAsync(() => upload.UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3, 4], "cv.pdf", contentType: "application/pdf")));

    cut.Find(".cv-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var items = await Service.ListAsync();
    while (DateTime.UtcNow < deadline && items.Count == 0)
    {
      await Task.Delay(50);
      items = await Service.ListAsync();
    }

    var item = Assert.Single(items);
    Assert.Equal("Fresh CV", item.Name);
    Assert.Equal("en", item.LanguageCode);
    var details = await Service.FindAsync(item.Id);
    Assert.Equal("cv.pdf", Assert.Single(details!.Versions).FileName);
  }
}
