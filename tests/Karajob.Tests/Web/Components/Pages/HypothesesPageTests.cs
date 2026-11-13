using Bunit;
using Karajob.Tests.Web.Components.Hypotheses;
using HypothesesPage = Karajob.Web.Components.Pages.Hypotheses;

namespace Karajob.Tests.Web.Components.Pages;

public class HypothesesPageTests : HypothesisWebFixture
{
  [Fact]
  public async Task Render_ShowsRowPerHypothesisName()
  {
    await Service.CreateAsync(Input("Unity Developer RU"));
    await Service.CreateAsync(Input("Unreal Developer EN"));

    var cut = Context.Render<HypothesesPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Unity Developer RU", cut.Markup);
      Assert.Contains("Unreal Developer EN", cut.Markup);
    });
  }

  [Fact]
  public async Task Render_LinksEachHypothesisToItsId()
  {
    var created = await Service.CreateAsync(Input("Unity Developer RU"));

    var cut = Context.Render<HypothesesPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith($"/hypotheses/{created.Id}", StringComparison.OrdinalIgnoreCase)));
  }

  [Fact]
  public void Render_HasLinkToNewPage()
  {
    var cut = Context.Render<HypothesesPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith("/hypotheses/new", StringComparison.Ordinal)));
  }
}
