using AngleSharp.Dom;
using Bunit;
using Karajob.Application.Cvs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Cvs;
using MudBlazor;
using TagsPage = Karajob.Web.Components.Pages.Tags;

namespace Karajob.Tests.Web.Components.Pages;

public class TagsPageTests : CvWebFixture
{
  private IRenderedComponent<MudDialogProvider> dialogs = null!;

  private IRenderedComponent<TagsPage> RenderPage()
  {
    Context.Render<MudPopoverProvider>();
    dialogs = Context.Render<MudDialogProvider>();
    return Context.Render<TagsPage>();
  }

  private static IElement Row(IRenderedComponent<TagsPage> cut, string name)
  {
    return cut.FindAll(".tag-row").Single(r => r.TextContent.Contains(name));
  }

  private static void SetText(IElement input, string value)
  {
    input.Input(value);
    input.Change(value);
  }

  private async Task<IReadOnlyList<TagListItem>> WaitForTagsAsync(Func<IReadOnlyList<TagListItem>, bool> condition)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var items = await Tags.ListAsync();
    while (DateTime.UtcNow < deadline && !condition(items))
    {
      await Task.Delay(50);
      items = await Tags.ListAsync();
    }

    return items;
  }

  [Fact]
  public void Render_NoTags_ShowsEmptyState()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Contains("No tags yet", cut.Markup));
    Assert.Empty(cut.FindAll(".tag-row"));
  }

  [Fact]
  public async Task Render_WithTags_DoesNotShowEmptyState()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");

    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));
    Assert.DoesNotContain("No tags yet", cut.Markup);
  }

  [Fact]
  public async Task Render_Tags_ShowsRowPerTagSortedByNameWithNameChip()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var chips = cut.FindAll(".tag-row .mud-chip").Select(c => c.TextContent.Trim()).ToList();
      Assert.Equal(["games", "unity"], chips);
    });
  }

  [Fact]
  public async Task Render_TagUsedByTwoCvs_ShowsCvCountInRow()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "One", unity, games);
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Two", unity);

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("2", Row(cut, "unity").TextContent);
      Assert.DoesNotContain("2", Row(cut, "games").TextContent);
      Assert.Contains("1", Row(cut, "games").TextContent);
    });
  }

  [Fact]
  public async Task Render_ColoredTag_ChipHasBackgroundColorStyle()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var colored = Row(cut, "unity").QuerySelector(".mud-chip")!;
      var plain = Row(cut, "games").QuerySelector(".mud-chip")!;
      Assert.Contains("#112233", colored.GetAttribute("style") ?? "", StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("#112233", plain.GetAttribute("style") ?? "", StringComparison.OrdinalIgnoreCase);
    });
  }

  [Fact]
  public async Task Render_TagsOfAnotherUser_AreNotShown()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddTagAsync(Database, other.Id, "foreign");
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");

    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));
    Assert.DoesNotContain("foreign", cut.Markup);
  }

  [Fact]
  public async Task Delete_Clicked_AsksForConfirmationAndKeepsTagUntilConfirmed()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-delete").Click();

    dialogs.WaitForElement(".mud-message-box__yes-button");
    Assert.Single(await Tags.ListAsync());
  }

  [Fact]
  public async Task Delete_Confirmed_RemovesTagAndRowButKeepsCv()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity, games);
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".tag-row").Count));

    Row(cut, "unity").QuerySelector(".tag-delete")!.Click();
    dialogs.WaitForElement(".mud-message-box__yes-button").Click();

    var items = await WaitForTagsAsync(i => i.Count == 1);
    Assert.Equal(["games"], items.Select(i => i.Name));
    cut.WaitForAssertion(() => Assert.Equal(["games"], cut.FindAll(".tag-row .mud-chip").Select(c => c.TextContent.Trim())));
    var cv = Assert.Single(await Service.ListAsync());
    Assert.Equal(["games"], cv.Tags.Select(t => t.Name));
  }

  [Fact]
  public async Task Delete_Cancelled_KeepsTag()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-delete").Click();
    dialogs.WaitForElement(".mud-message-box__cancel-button").Click();
    await Task.Delay(300);

    Assert.Single(await Tags.ListAsync());
    Assert.Single(cut.FindAll(".tag-row"));
  }

  [Fact]
  public async Task Color_ButtonClicked_OpensDialogPrefilledWithCurrentColor()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-color").Click();

    var input = dialogs.WaitForElement(".tag-color-input input");
    Assert.Equal("#112233", input.GetAttribute("value"));
  }

  [Fact]
  public async Task Color_ValidHexSaved_PersistsColorAndUpdatesRow()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-color").Click();
    SetText(dialogs.WaitForElement(".tag-color-input input"), "#A1B2C3");
    dialogs.Find(".tag-color-save").Click();

    var items = await WaitForTagsAsync(i => i[0].Color is not null);
    Assert.Equal("#a1b2c3", Assert.Single(items).Color);
    cut.WaitForAssertion(() =>
      Assert.Contains("#a1b2c3", Row(cut, "unity").QuerySelector(".mud-chip")!.GetAttribute("style") ?? "", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task Color_InvalidHexSaved_DoesNotChangeColorAndKeepsDialogOpen()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-color").Click();
    SetText(dialogs.WaitForElement(".tag-color-input input"), "not-a-color");
    dialogs.Find(".tag-color-save").Click();
    await Task.Delay(300);

    Assert.Equal("#112233", Assert.Single(await Tags.ListAsync()).Color);
    Assert.NotEmpty(dialogs.FindAll(".tag-color-input"));
  }

  [Fact]
  public async Task Color_ClearClicked_RemovesColor()
  {
    await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    var cut = RenderPage();
    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".tag-row")));

    cut.Find(".tag-row .tag-color").Click();
    dialogs.WaitForElement(".tag-color-clear").Click();

    var items = await WaitForTagsAsync(i => i[0].Color is null);
    Assert.Null(Assert.Single(items).Color);
  }
}
