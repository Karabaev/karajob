using Bunit;
using Karajob.Application.MasterProfiles;
using Karajob.Tests.Application.MasterProfiles;
using Karajob.Web.Components.MasterProfiles;
using MudBlazor;

namespace Karajob.Tests.Web.Components.MasterProfiles;

public class MasterProfilePageTests : MasterProfileWebFixture
{
  private IRenderedComponent<MudDialogProvider> dialogs = null!;

  private IRenderedComponent<MasterProfilePage> RenderPage()
  {
    Context.Render<MudPopoverProvider>();
    dialogs = Context.Render<MudDialogProvider>();
    return Context.Render<MasterProfilePage>();
  }

  private static string EditorText(IRenderedComponent<MasterProfilePage> cut)
  {
    var textarea = cut.Find(".master-profile-editor textarea");
    return textarea.GetAttribute("value") ?? textarea.TextContent;
  }

  private static void SetEditor(IRenderedComponent<MasterProfilePage> cut, string text)
  {
    cut.WaitForElement(".master-profile-editor textarea").Input(text);
  }

  private async Task<MasterProfileDetails> WaitForVersionsAsync(int count)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var details = await Service.GetAsync();
    while (DateTime.UtcNow < deadline && details.Versions.Count < count)
    {
      await Task.Delay(50);
      details = await Service.GetAsync();
    }

    return details;
  }

  [Fact]
  public void Render_FirstOpen_ShowsTemplateInEditor()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut)));
  }

  [Fact]
  public void Render_FirstOpen_ShowsSingleCurrentVersion()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var row = Assert.Single(cut.FindAll(".master-profile-version"));
      Assert.Contains("master-profile-version-current", row.ClassName);
    });
  }

  [Fact]
  public void Render_FirstOpen_HidesUnsavedIndicator()
  {
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut)));

    Assert.Empty(cut.FindAll(".master-profile-unsaved"));
  }

  [Fact]
  public void Edit_ChangedText_ShowsUnsavedIndicator()
  {
    var cut = RenderPage();

    SetEditor(cut, "# Changed");

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".master-profile-unsaved")));
  }

  [Fact]
  public void Edit_TextRevertedToSaved_HidesUnsavedIndicator()
  {
    var cut = RenderPage();
    SetEditor(cut, "# Changed");
    cut.WaitForElement(".master-profile-unsaved");

    SetEditor(cut, MasterProfileTestTemplate.English);

    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".master-profile-unsaved")));
  }

  [Fact]
  public async Task Save_ChangedTextAndComment_AddsCurrentVersionWithCommentAndClearsIndicator()
  {
    var cut = RenderPage();
    SetEditor(cut, "# Changed");
    cut.Find(".master-profile-comment input").Input("my comment");

    cut.Find(".master-profile-save").Click();

    var details = await WaitForVersionsAsync(2);
    Assert.Equal("# Changed", details.Markdown);
    Assert.Equal("my comment", details.Versions[0].Comment);
    cut.WaitForAssertion(() =>
    {
      var rows = cut.FindAll(".master-profile-version");
      Assert.Equal(2, rows.Count);
      Assert.Contains("master-profile-version-current", rows[0].ClassName);
      Assert.DoesNotContain("master-profile-version-current", rows[1].ClassName);
      Assert.Contains("my comment", rows[0].TextContent);
      Assert.Empty(cut.FindAll(".master-profile-unsaved"));
    });
  }

  [Fact]
  public void Save_UnchangedText_ShowsNoChangesError()
  {
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut)));

    cut.Find(".master-profile-save").Click();

    cut.WaitForAssertion(() => Assert.Contains("No changes", cut.Markup));
  }

  [Fact]
  public async Task Save_UnchangedText_AddsNoVersion()
  {
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut)));

    cut.Find(".master-profile-save").Click();
    cut.WaitForAssertion(() => Assert.Contains("No changes", cut.Markup));

    var details = await Service.GetAsync();
    Assert.Single(details.Versions);
  }

  [Fact]
  public void PreviewTab_TemplateText_RendersHeadingsInPreview()
  {
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut)));

    cut.FindAll("[role=tab]").First(t => t.TextContent.Contains("Preview")).Click();

    cut.WaitForAssertion(() => Assert.Contains("<h2", cut.Find(".master-profile-preview").InnerHtml));
  }

  [Fact]
  public async Task Restore_OlderVersionConfirmed_AddsRestoredVersionWithOldText()
  {
    var first = await Service.GetAsync();
    await Service.SaveAsync(new MasterProfileInput("# Second", null));
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal("# Second", EditorText(cut)));

    cut.FindAll(".master-profile-version")[1].QuerySelector(".master-profile-restore")!.Click();
    dialogs.WaitForAssertion(() => Assert.Contains(dialogs.FindAll("button"), b => b.TextContent.Contains("Restore")));
    dialogs.FindAll("button").First(b => b.TextContent.Contains("Restore")).Click();

    var details = await WaitForVersionsAsync(3);
    Assert.Equal(MasterProfileTestTemplate.English, details.Markdown);
    Assert.Equal(first.CurrentVersionId, details.Versions[0].RestoredFromVersionId);
    cut.WaitForAssertion(() =>
    {
      Assert.Equal(MasterProfileTestTemplate.English, EditorText(cut));
      var rows = cut.FindAll(".master-profile-version");
      Assert.Equal(3, rows.Count);
      Assert.NotNull(rows[0].QuerySelector(".master-profile-restored-from"));
      Assert.Contains("master-profile-version-current", rows[0].ClassName);
    });
  }

  [Fact]
  public async Task Render_TwoVersions_CurrentRowHasNoRestoreButton()
  {
    await Service.GetAsync();
    await Service.SaveAsync(new MasterProfileInput("# Second", null));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var rows = cut.FindAll(".master-profile-version");
      Assert.Equal(2, rows.Count);
      Assert.Null(rows[0].QuerySelector(".master-profile-restore"));
      Assert.NotNull(rows[0].QuerySelector(".master-profile-view"));
      Assert.NotNull(rows[1].QuerySelector(".master-profile-restore"));
      Assert.NotNull(rows[1].QuerySelector(".master-profile-view"));
    });
  }
}
