using Bunit;
using Microsoft.AspNetCore.Components;
using CompaniesPage = Karajob.Web.Components.Pages.Companies;

namespace Karajob.Tests.Web.Components.Companies;

public class CompaniesPageTests : CompanyWebFixture
{
  [Fact]
  public async Task Render_ShowsRowPerCompanyName()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    await Service.CreateAsync(Input("lesta-igry", "Леста Игры"));

    var cut = Context.Render<CompaniesPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Pixel Forge", cut.Markup);
      Assert.Contains("Леста Игры", cut.Markup);
    });
  }

  [Fact]
  public async Task Render_LinksEachCompanyToItsSlug()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));

    var cut = Context.Render<CompaniesPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll("a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith("/companies/pixel-forge", StringComparison.Ordinal)));
  }

  [Fact]
  public void Render_HasAddCompanyLinkToNewPage()
  {
    var cut = Context.Render<CompaniesPage>();

    var link = cut.FindAll("a").First(a => a.TextContent.Contains("+ Компания"));
    Assert.EndsWith("/companies/new", link.GetAttribute("href")!.TrimEnd('/'));
  }

  [Fact]
  public async Task TypeInSearch_FiltersCompanies()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    await Service.CreateAsync(Input("lesta-igry", "Леста Игры"));
    var cut = Context.Render<CompaniesPage>();
    cut.WaitForAssertion(() => Assert.Contains("Pixel Forge", cut.Markup));

    cut.Find(".company-search input").Input("лест");

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Леста Игры", cut.Markup);
      Assert.DoesNotContain("Pixel Forge", cut.Markup);
    }, TimeSpan.FromSeconds(5));
  }
}
