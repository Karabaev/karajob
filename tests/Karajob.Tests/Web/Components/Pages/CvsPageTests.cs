using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Cvs;
using CvsPage = Karajob.Web.Components.Pages.Cvs;

namespace Karajob.Tests.Web.Components.Pages;

public class CvsPageTests : CvWebFixture
{
  [Fact]
  public async Task Render_ShowsEachCvNameAndItsTags()
  {
    await Service.CreateAsync(Input("Unity CV", ["unity", "games"]), Upload());
    await Service.CreateAsync(Input("Unreal CV", ["unreal"]), Upload());

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Unity CV", cut.Markup);
      Assert.Contains("Unreal CV", cut.Markup);
      Assert.Contains("games", cut.Markup);
      Assert.Contains("unreal", cut.Markup);
    });
  }

  [Fact]
  public async Task Render_LinksEachCvToItsId()
  {
    var created = await Service.CreateAsync(Input("Unity CV"), Upload());

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith($"/cvs/{created.Id}", StringComparison.OrdinalIgnoreCase)));
  }

  [Fact]
  public void Render_HasLinkToNewPage()
  {
    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith("/cvs/new", StringComparison.Ordinal)));
  }

  [Fact]
  public void Render_NoCvs_ShowsEmptyState()
  {
    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() => Assert.Contains("No CVs yet", cut.Markup));
  }

  [Fact]
  public async Task Render_WithCvs_DoesNotShowEmptyState()
  {
    await Service.CreateAsync(Input("Unity CV"), Upload());

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() => Assert.Contains("Unity CV", cut.Markup));
    Assert.DoesNotContain("No CVs yet", cut.Markup);
  }

  [Fact]
  public async Task Render_CvTags_ShowOneChipPerTagSortedByName()
  {
    var unity = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity");
    var games = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", unity, games);

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() =>
      Assert.Equal(["games", "unity"], cut.FindAll(".cv-tag-chip").Select(c => c.TextContent.Trim())));
  }

  [Fact]
  public async Task Render_TagWithColor_ChipHasBackgroundColorStyle()
  {
    var colored = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "unity", "#112233");
    var plain = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", colored, plain);

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() =>
    {
      var chips = cut.FindAll(".cv-tag-chip");
      var unityChip = chips.Single(c => c.TextContent.Trim() == "unity");
      var gamesChip = chips.Single(c => c.TextContent.Trim() == "games");
      var style = unityChip.GetAttribute("style") ?? "";
      Assert.Contains("background", style, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("#112233", style, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("#112233", gamesChip.GetAttribute("style") ?? "", StringComparison.OrdinalIgnoreCase);
    });
  }

  [Fact]
  public async Task Render_TagWithoutColor_ChipHasNoBackgroundOverride()
  {
    var plain = await PersistenceFixtures.AddTagAsync(Database, SeedData.OwnerId, "games");
    await PersistenceFixtures.AddCvWithTagsAsync(Database, SeedData.OwnerId, "Main", plain);

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() =>
    {
      var chip = Assert.Single(cut.FindAll(".cv-tag-chip"));
      Assert.DoesNotContain("background", chip.GetAttribute("style") ?? "", StringComparison.OrdinalIgnoreCase);
    });
  }
}
