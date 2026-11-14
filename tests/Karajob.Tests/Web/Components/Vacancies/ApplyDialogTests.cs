using Bunit;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class ApplyDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Submit_WithPrimaryCvOfHypothesis_AppliesWithNewestVersionAndCloses()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var (documentId, firstVersionId) = await CreateCvAsync("Primary", "v1.pdf");
    await SetPrimaryCvAsync(hypothesisId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-submit");

    provider.Find(".apply-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var vacancy = await FindWithApplicationAsync(vacancyId);
    Assert.Equal(VacancyStage.Applied, vacancy.Stage);
    Assert.Equal(hypothesisId, vacancy.Application!.HypothesisId);
    Assert.Equal(firstVersionId, vacancy.Application.CvVersionId);
  }

  [Fact]
  public async Task Submit_WithFutureDate_ShowsErrorAndStaysOpenWithoutApplying()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var (documentId, _) = await CreateCvAsync();
    await SetPrimaryCvAsync(hypothesisId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-date");
    var picker = provider.FindComponents<MudDatePicker>().Single(c => c.Instance.Class!.Contains("apply-date"));
    await provider.InvokeAsync(() => picker.Instance.DateChanged.InvokeAsync(DateTime.UtcNow.Date.AddDays(5)));

    provider.Find(".apply-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-alert, .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Equal(VacancyStage.Preparing, (await FindWithApplicationAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Submit_VacancyMovedMeanwhile_ShowsErrorAndStaysOpen()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var (documentId, _) = await CreateCvAsync();
    await SetPrimaryCvAsync(hypothesisId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-submit");
    await Vacancies.MoveAsync(vacancyId, VacancyStage.Preparing, VacancyStage.Targeted);

    provider.Find(".apply-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-alert")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Equal(VacancyStage.Targeted, (await FindWithApplicationAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Cancel_ClosesDialogAsCanceledAndLeavesStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-cancel").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(result!.Canceled);
    Assert.Equal(VacancyStage.Preparing, (await FindWithApplicationAsync(vacancyId)).Stage);
  }

  private async Task<Vacancy> FindWithApplicationAsync(Guid vacancyId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies.Include(v => v.Application).SingleAsync(v => v.Id == vacancyId);
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId, VacancyStage expectedStage)
  {
    return ShowDialogAsync(new DialogParameters<ApplyDialog>
    {
      { d => d.VacancyId, vacancyId },
      { d => d.ExpectedStage, expectedStage },
    });
  }
}
