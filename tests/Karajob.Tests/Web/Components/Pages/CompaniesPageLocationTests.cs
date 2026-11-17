using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Companies;
using MudBlazor;
using CompaniesPage = Karajob.Web.Components.Pages.Companies;

namespace Karajob.Tests.Web.Components.Pages;

public class CompaniesPageLocationTests : CompanyWebFixture
{
  [Fact]
  public async Task Render_CompanyWithLocations_ShowsChipPerLocationName()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge") with { LocationIds = [SeedData.UsaLocationId, SeedData.EuLocationId] });

    var cut = Context.Render<CompaniesPage>();

    cut.WaitForAssertion(() =>
    {
      var chips = cut.FindComponents<MudChip<string>>().Select(c => c.Markup).ToList();
      Assert.Contains(chips, m => m.Contains("EU"));
      Assert.Contains(chips, m => m.Contains("USA"));
    });
  }

  [Fact]
  public async Task Render_ShowsLocationsColumnHeader()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));

    var cut = Context.Render<CompaniesPage>();

    cut.WaitForAssertion(() => Assert.Contains("Locations", cut.Markup));
  }
}
