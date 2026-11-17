using Bunit;
using Karajob.Application.Companies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Companies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Companies;

public class CompanyPageLocationTests : CompanyWebFixture
{
  [Fact]
  public async Task Render_ExistingCompany_ShowsStoredLocations()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge") with { LocationIds = [SeedData.EuLocationId, SeedData.SerbiaLocationId] });

    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(),
      LocationsSelect(cut).Instance.SelectedValues!.Order()));
  }

  [Fact]
  public async Task Save_ChangedLocations_PersistsNewSelection()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge") with { LocationIds = [SeedData.EuLocationId] });
    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));
    cut.WaitForAssertion(() => Assert.Equal("Pixel Forge", cut.Find(".company-name input").GetAttribute("value")));

    await cut.InvokeAsync(() => LocationsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.UsaLocationId, SeedData.SerbiaLocationId]));
    cut.Find(".company-save").Click();

    await WaitForLocationsAsync([SeedData.SerbiaLocationId, SeedData.UsaLocationId]);
  }

  [Fact]
  public async Task Save_NewCompanyWithLocations_PersistsLocations()
  {
    var cut = Context.Render<CompanyPage>();
    cut.Find(".company-name input").Input("Pixel Forge");
    cut.WaitForAssertion(() => Assert.Equal("pixel-forge", cut.Find(".company-slug input").GetAttribute("value")));
    await cut.InvokeAsync(() => LocationsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.EuLocationId]));

    cut.Find(".company-save").Click();

    await WaitForLocationsAsync([SeedData.EuLocationId]);
  }

  private async Task WaitForLocationsAsync(IEnumerable<Guid> expected)
  {
    var wanted = expected.Order().ToList();
    for (var attempt = 0; attempt < 50; attempt++)
    {
      var company = await Service.FindBySlugAsync("pixel-forge");
      if (company is not null && company.LocationIds.Order().SequenceEqual(wanted))
      {
        return;
      }

      await Task.Delay(100);
    }

    Assert.Equal(wanted, (await Service.FindBySlugAsync("pixel-forge"))?.LocationIds.Order());
  }

  private static IRenderedComponent<MudSelect<Guid>> LocationsSelect(IRenderedComponent<CompanyPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class!.Contains("company-locations"));
  }
}
