using Bunit;
using Karajob.Domain.Vacancies;
using Karajob.Web.Components.Vacancies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class DismissVacancyDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Submit_WithDefaultReason_DismissesVacancyAndCloses()
  {
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowAsync(vacancy.Id);

    provider.WaitForElement(".dismiss-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var found = await Vacancies.FindAsync(vacancy.Id);
    Assert.Equal(VacancyStage.Dismissed, found!.Stage);
    Assert.NotNull(found.DismissReason);
  }

  [Fact]
  public async Task Submit_WithComment_StoresCommentOnStageEvent()
  {
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowAsync(vacancy.Id);
    provider.WaitForElement(".dismiss-comment textarea, .dismiss-comment input").Input("too far");

    provider.Find(".dismiss-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var found = await Vacancies.FindAsync(vacancy.Id);
    Assert.Equal("too far", Assert.Single(found!.Timeline).Note);
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId)
  {
    return ShowDialogAsync(new DialogParameters<DismissVacancyDialog> { { d => d.VacancyId, vacancyId } });
  }
}
