using Bunit;
using Karajob.Application.Companies;
using Karajob.Domain.Companies;
using Karajob.Web.Components.Companies;

namespace Karajob.Tests.Web.Components.Companies;

public class CompanyPageTests : CompanyWebFixture
{
  [Fact]
  public void TypeName_OnNewPage_FillsSlugWithSlugifiedName()
  {
    var cut = Context.Render<CompanyPage>();

    cut.Find(".company-name input").Input("Pixel Forge");

    cut.WaitForAssertion(() => Assert.Equal("pixel-forge", cut.Find(".company-slug input").GetAttribute("value")));
  }

  [Fact]
  public async Task Save_SlugTakenByAnotherCompany_ShowsConflictMessageWithOtherName()
  {
    await Service.CreateAsync(Input("pixel-forge", "Taken Studio"));
    var cut = Context.Render<CompanyPage>();
    cut.Find(".company-name input").Input("Pixel Forge");
    cut.WaitForAssertion(() => Assert.Equal("pixel-forge", cut.Find(".company-slug input").GetAttribute("value")));

    cut.Find(".company-save").Click();

    cut.WaitForAssertion(() => Assert.Contains("Taken Studio", cut.Markup));
  }

  [Fact]
  public async Task Save_ValidNewCompany_PersistsCompany()
  {
    var cut = Context.Render<CompanyPage>();
    cut.Find(".company-name input").Input("Pixel Forge");
    cut.WaitForAssertion(() => Assert.Equal("pixel-forge", cut.Find(".company-slug input").GetAttribute("value")));

    cut.Find(".company-save").Click();

    await WaitForCompanyAsync("pixel-forge");
  }

  [Fact]
  public async Task Render_ExistingCompany_ShowsNameInNameInput()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));

    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));

    cut.WaitForAssertion(() => Assert.Equal("Pixel Forge", cut.Find(".company-name input").GetAttribute("value")));
  }

  [Fact]
  public async Task Render_ExistingCompany_ListsProjectsByName()
  {
    var company = await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    await Service.AddProjectAsync(company.Id, new ProjectInput("Dragon Quest", null, null, ProjectStatus.InDevelopment, null));
    await Service.AddProjectAsync(company.Id, new ProjectInput("Star Raiders", null, null, ProjectStatus.Released, null));

    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Dragon Quest", cut.Markup);
      Assert.Contains("Star Raiders", cut.Markup);
    });
  }

  [Fact]
  public async Task SaveCompany_ExistingCompany_KeepsUnsavedNote()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));
    cut.WaitForAssertion(() => Assert.Equal("Pixel Forge", cut.Find(".company-name input").GetAttribute("value")));
    cut.Find(".company-note textarea").Change("draft note");
    cut.Find(".company-name input").Input("Pixel Forge Studio");

    cut.Find(".company-save").Click();

    cut.WaitForAssertion(() => Assert.Equal("Pixel Forge Studio", cut.Find("h1").TextContent.Trim()));
    Assert.Equal("Pixel Forge Studio", (await Service.FindBySlugAsync("pixel-forge"))!.Name);
    Assert.Equal("draft note", cut.Find(".company-note textarea").GetAttribute("value"));
  }

  private async Task WaitForCompanyAsync(string slug)
  {
    for (var attempt = 0; attempt < 50; attempt++)
    {
      if (await Service.FindBySlugAsync(slug) is not null)
      {
        return;
      }

      await Task.Delay(100);
    }

    Assert.NotNull(await Service.FindBySlugAsync(slug));
  }
}
