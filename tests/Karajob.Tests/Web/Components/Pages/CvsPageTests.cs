using Bunit;
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

    cut.WaitForAssertion(() => Assert.Contains("CV пока нет", cut.Markup));
  }

  [Fact]
  public async Task Render_WithCvs_DoesNotShowEmptyState()
  {
    await Service.CreateAsync(Input("Unity CV"), Upload());

    var cut = Context.Render<CvsPage>();

    cut.WaitForAssertion(() => Assert.Contains("Unity CV", cut.Markup));
    Assert.DoesNotContain("CV пока нет", cut.Markup);
  }
}
