using AngleSharp.Dom;
using Bunit;
using Karajob.Application.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Components.Cvs;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Cvs;

public class CvPageTagTests : CvWebFixture
{
  private const string TagInputSelector = ".cv-tag-input input";
  private const string ChipSelector = ".cv-tag";

  private IRenderedComponent<MudPopoverProvider> popovers = null!;

  private IRenderedComponent<CvPage> RenderPage(Guid? id = null)
  {
    popovers = Context.Render<MudPopoverProvider>();
    Context.Render<MudDialogProvider>();
    return id is null
      ? Context.Render<CvPage>()
      : Context.Render<CvPage>(p => p.Add(c => c.Id, id.Value));
  }

  private static List<string> ChipTexts(IRenderedComponent<CvPage> cut)
  {
    return cut.FindAll(ChipSelector).Select(c => c.TextContent.Trim()).Order().ToList();
  }

  private static void Type(IRenderedComponent<CvPage> cut, string text)
  {
    var input = cut.WaitForElement(TagInputSelector);
    input.Input(text);
  }

  private static void TypeAndEnter(IRenderedComponent<CvPage> cut, string text)
  {
    Type(cut, text);
    cut.Find(TagInputSelector).KeyDown(new KeyboardEventArgs { Key = "Enter" });
  }

  private async Task<List<string>> WaitForTagsAsync(Guid cvId, Func<List<string>, bool> condition)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var names = new List<string>();
    while (DateTime.UtcNow < deadline)
    {
      var details = await Service.FindAsync(cvId);
      names = details!.Tags.Select(t => t.Name).ToList();
      if (condition(names))
      {
        break;
      }

      await Task.Delay(50);
    }

    return names;
  }

  [Fact]
  public void Render_NewCv_HasTagAutocompleteAndNoLegacyMultilineField()
  {
    var cut = RenderPage();

    cut.WaitForElement(TagInputSelector);
    Assert.Empty(cut.FindAll(".cv-tags"));
    Assert.Empty(cut.FindAll(ChipSelector));
  }

  [Fact]
  public async Task Render_CvWithTags_ShowsChipPerTagSortedByName()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity, games);

    var cut = RenderPage(document.Id);

    cut.WaitForAssertion(() => Assert.Equal(["games", "unity"], ChipTexts(cut)));
  }

  [Fact]
  public void Type_NameAndEnter_AddsChip()
  {
    var cut = RenderPage();

    TypeAndEnter(cut, "unity");

    cut.WaitForAssertion(() => Assert.Equal(["unity"], ChipTexts(cut)));
  }

  [Fact]
  public void Type_NameFollowedByComma_AddsChip()
  {
    var cut = RenderPage();

    Type(cut, "unity,");

    cut.WaitForAssertion(() => Assert.Equal(["unity"], ChipTexts(cut)));
  }

  [Fact]
  public void Type_NameAndEnter_ClearsInput()
  {
    var cut = RenderPage();

    TypeAndEnter(cut, "unity");

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(ChipSelector));
      Assert.Equal("", cut.Find(TagInputSelector).GetAttribute("value") ?? "");
    });
  }

  [Fact]
  public void Type_TwoNames_AddsTwoChips()
  {
    var cut = RenderPage();

    TypeAndEnter(cut, "unity");
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
    Type(cut, "games,");

    cut.WaitForAssertion(() => Assert.Equal(["games", "unity"], ChipTexts(cut)));
  }

  [Fact]
  public void Type_SameNameTwice_KeepsSingleChip()
  {
    var cut = RenderPage();

    TypeAndEnter(cut, "unity");
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
    TypeAndEnter(cut, "unity");

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
  }

  [Fact]
  public void Type_BlankAndEnter_AddsNoChip()
  {
    var cut = RenderPage();

    TypeAndEnter(cut, "   ");

    Assert.Empty(cut.FindAll(ChipSelector));
  }

  [Fact]
  public async Task Chip_CloseClicked_RemovesChip()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity, games);
    var cut = RenderPage(document.Id);
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(ChipSelector).Count));

    var chip = cut.FindAll(ChipSelector).Single(c => c.TextContent.Trim() == "unity");
    chip.QuerySelector(".mud-chip-close-button")!.Click();

    cut.WaitForAssertion(() => Assert.Equal(["games"], ChipTexts(cut)));
  }

  [Fact]
  public async Task Save_NewCvWithChips_PersistsChipNamesAsTags()
  {
    var cut = RenderPage();
    cut.WaitForElement(".cv-name input").Input("Fresh CV");
    TypeAndEnter(cut, "Unity");
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
    Type(cut, "games,");
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(ChipSelector).Count));
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
    Assert.Equal(["games", "unity"], item.Tags.Select(t => t.Name).Order());
  }

  [Fact]
  public async Task Save_ExistingCvAfterRemovingAndAddingChips_UpdatesTags()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity, games);
    var cut = RenderPage(document.Id);
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(ChipSelector).Count));
    cut.FindAll(ChipSelector).Single(c => c.TextContent.Trim() == "games").QuerySelector(".mud-chip-close-button")!.Click();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
    TypeAndEnter(cut, "unreal");
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(ChipSelector).Count));

    cut.Find(".cv-save").Click();

    var names = await WaitForTagsAsync(document.Id, n => n.Contains("unreal"));
    Assert.Equal(["unity", "unreal"], names.Order());
  }

  [Fact]
  public async Task Save_ExistingCvWithoutChanges_KeepsTags()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var document = await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity);
    var cut = RenderPage(document.Id);
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(ChipSelector)));
    cut.WaitForElement(".cv-name input").Input("Renamed");

    cut.Find(".cv-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var details = await Service.FindAsync(document.Id);
    while (DateTime.UtcNow < deadline && details!.Name != "Renamed")
    {
      await Task.Delay(50);
      details = await Service.FindAsync(document.Id);
    }

    Assert.Equal("Renamed", details!.Name);
    Assert.Equal(unity.Id, Assert.Single(details.Tags).Id);
  }

  [Fact]
  public async Task Type_Prefix_SuggestsMatchingExistingTagsFromTagService()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unreal");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    var cut = RenderPage();
    var input = cut.WaitForElement(TagInputSelector);

    await cut.InvokeAsync(() => input.Focus());
    input.Input("un");

    cut.WaitForAssertion(() =>
    {
      var suggestions = popovers.FindAll(".mud-popover-open .mud-list-item").Select(e => e.TextContent.Trim()).ToList();
      Assert.Contains("unity", suggestions);
      Assert.Contains("unreal", suggestions);
      Assert.DoesNotContain("games", suggestions);
    });
  }

  [Fact]
  public async Task Type_OtherUsersTagPrefix_SuggestsNothingForCurrentUser()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddTagAsync(Database, other.Id, "unity");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unreal");
    var cut = RenderPage();
    var input = cut.WaitForElement(TagInputSelector);

    await cut.InvokeAsync(() => input.Focus());
    input.Input("un");

    cut.WaitForAssertion(() =>
    {
      var suggestions = popovers.FindAll(".mud-popover-open .mud-list-item").Select(e => e.TextContent.Trim()).ToList();
      Assert.Contains("unreal", suggestions);
      Assert.DoesNotContain("unity", suggestions);
    });
  }
}
