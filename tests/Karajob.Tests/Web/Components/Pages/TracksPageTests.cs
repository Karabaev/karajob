using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Tracks;
using TracksPage = Karajob.Web.Components.Pages.Tracks;

namespace Karajob.Tests.Web.Components.Pages;

public class TracksPageTests : TrackWebFixture
{
  [Fact]
  public async Task Render_ShowsRowPerTrackName()
  {
    await Service.CreateAsync(Input("Unity Developer RU"));
    await Service.CreateAsync(Input("Unreal Developer EN"));

    var cut = Context.Render<TracksPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Unity Developer RU", cut.Markup);
      Assert.Contains("Unreal Developer EN", cut.Markup);
    });
  }

  [Fact]
  public async Task Render_LinksEachTrackToItsId()
  {
    var created = await Service.CreateAsync(Input("Unity Developer RU"));

    var cut = Context.Render<TracksPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith($"/tracks/{created.Id}", StringComparison.OrdinalIgnoreCase)));
  }

  [Fact]
  public void Render_HasLinkToNewPage()
  {
    var cut = Context.Render<TracksPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith("/tracks/new", StringComparison.Ordinal)));
  }

  [Fact]
  public async Task Render_ShowsRoleNameAndUpperCaseLanguageCodeInRow()
  {
    await Service.CreateAsync(Input("Unreal EN", SeedData.UnrealDeveloperRoleId) with { LanguageId = SeedData.EnglishLanguageId });

    var cut = Context.Render<TracksPage>();

    cut.WaitForAssertion(() =>
    {
      var cells = cut.FindAll("tbody tr td").Select(td => td.TextContent.Trim()).ToList();
      Assert.Contains("Unreal Developer", cells);
      Assert.Contains("EN", cells);
    });
  }
}
