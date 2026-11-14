using Bunit;
using Karajob.Application.Companies;
using Karajob.Domain.Normalization;
using Karajob.Web.Components.Vacancies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class CreateCompanyDialogTests : VacancyWebFixture
{
  private static DialogParameters<CreateCompanyDialog> ParametersFor(string initialName)
  {
    return new DialogParameters<CreateCompanyDialog> { { d => d.InitialName, initialName } };
  }

  [Fact]
  public async Task Render_WithInitialName_PrefillsSlug()
  {
    var (provider, _) = await ShowDialogAsync(ParametersFor("Pixel Forge"));

    provider.WaitForAssertion(() =>
      Assert.Equal(Normalize.Slugify("Pixel Forge"), provider.Find(".company-slug input").GetAttribute("value")));
  }

  [Fact]
  public async Task Save_ValidName_CreatesCompanyAndClosesWithDetails()
  {
    var (provider, reference) = await ShowDialogAsync(ParametersFor("Pixel Forge"));
    provider.WaitForElement(".company-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.False(result!.Canceled);
    var details = Assert.IsType<CompanyDetails>(result.Data);
    Assert.Equal("pixel-forge", details.Slug);
    Assert.Equal("Pixel Forge", details.Name);
    Assert.NotNull(await Companies.FindBySlugAsync("pixel-forge"));
  }

  [Fact]
  public async Task Save_SlugTakenByAnotherCompany_ShowsConflictAndStaysOpen()
  {
    await Companies.CreateAsync(new CompanyInput("pixel-forge", "Taken Studio", null, null, [], []));
    var (provider, reference) = await ShowDialogAsync(ParametersFor("Pixel Forge"));
    provider.WaitForElement(".company-save").Click();

    provider.WaitForAssertion(() => Assert.Contains("Taken Studio", provider.Markup));
    Assert.False(reference.Result!.IsCompleted);
  }
}
