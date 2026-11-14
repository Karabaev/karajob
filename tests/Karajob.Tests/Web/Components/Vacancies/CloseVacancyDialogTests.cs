using Bunit;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class CloseVacancyDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Submit_WithChosenOutcome_ClosesVacancyWithThatOutcomeAndDialog()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Interviewing);
    provider.WaitForElement(".close-outcome");
    var select = provider.FindComponents<MudSelect<ApplicationOutcome>>().Single(c => c.Instance.Class!.Contains("close-outcome"));
    await provider.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(ApplicationOutcome.Offer));

    provider.Find(".close-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.Equal(ApplicationOutcome.Offer, vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task Submit_WithDefaultOutcome_ClosesVacancyWithSomeOutcome()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Applied);

    provider.WaitForElement(".close-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.NotNull(vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task Submit_VacancyMovedMeanwhile_ShowsErrorAndStaysOpen()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Applied);
    provider.WaitForElement(".close-submit");
    await Vacancies.MoveAsync(vacancyId, VacancyStage.Applied, VacancyStage.Interviewing);

    provider.Find(".close-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-alert")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Equal(VacancyStage.Interviewing, (await LoadAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Cancel_ClosesDialogAsCanceledAndLeavesStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Interviewing);
    provider.WaitForElement(".close-cancel").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(result!.Canceled);
    Assert.Equal(VacancyStage.Interviewing, (await LoadAsync(vacancyId)).Stage);
  }

  private async Task<Vacancy> LoadAsync(Guid vacancyId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies.Include(v => v.Application).SingleAsync(v => v.Id == vacancyId);
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId, VacancyStage expectedStage)
  {
    return ShowDialogAsync(new DialogParameters<CloseVacancyDialog>
    {
      { d => d.VacancyId, vacancyId },
      { d => d.ExpectedStage, expectedStage },
    });
  }
}
