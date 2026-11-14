using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Cvs;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Cvs;

public class CvPageTests : CvWebFixture
{
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
  public void Render_UnknownId_ShowsNotFound()
  {
    var cut = Context.Render<CvPage>(p => p.Add(c => c.Id, Guid.NewGuid()));

    cut.WaitForAssertion(() => Assert.Contains("CV не найдено", cut.Markup));
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
