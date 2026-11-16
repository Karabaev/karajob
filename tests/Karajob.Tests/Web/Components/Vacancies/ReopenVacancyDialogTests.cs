using Bunit;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class ReopenVacancyDialogTests : VacancyWebFixture
{
  [Theory]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public async Task Render_TargetSelect_StartsWithDefaultTarget(VacancyStage defaultTarget)
  {
    var vacancyId = await CreateClosedAsync();

    var (provider, _) = await ShowAsync(vacancyId, defaultTarget);

    provider.WaitForElement(".reopen-target");
    Assert.Equal(defaultTarget, TargetSelect(provider).Instance.Value);
  }

  [Fact]
  public async Task Submit_WithDefaultTarget_ReopensVacancyAndClosesDialogWithOk()
  {
    var vacancyId = await CreateClosedAsync();
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Interviewing);

    provider.WaitForElement(".reopen-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    Assert.Equal(true, result.Data);
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Interviewing, vacancy.Stage);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task Submit_WithChangedTarget_ReopensVacancyAtChosenStage()
  {
    var vacancyId = await CreateClosedAsync();
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Interviewing);
    provider.WaitForElement(".reopen-target");
    await provider.InvokeAsync(() => TargetSelect(provider).Instance.ValueChanged.InvokeAsync(VacancyStage.Applied));

    provider.Find(".reopen-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal(VacancyStage.Applied, (await LoadAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Submit_VacancyNoLongerClosed_ShowsErrorAndStaysOpen()
  {
    var vacancyId = await CreateClosedAsync();
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Applied);
    provider.WaitForElement(".reopen-submit");
    await MutateAsync(vacancyId, v => v.Reopen(VacancyStage.Interviewing, DateTime.UtcNow));

    provider.Find(".reopen-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-alert")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Equal(VacancyStage.Interviewing, (await LoadAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Cancel_ClosesDialogAsCanceledAndKeepsVacancyClosed()
  {
    var vacancyId = await CreateClosedAsync();
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Applied);
    provider.WaitForElement(".reopen-cancel").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(result!.Canceled);
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.Equal(ApplicationOutcome.Offer, vacancy.Application!.Outcome);
  }

  private async Task<Guid> CreateClosedAsync()
  {
    var hypothesisId = await CreateHypothesisAsync();
    return await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1", ApplicationOutcome.Offer);
  }

  private static IRenderedComponent<MudSelect<VacancyStage>> TargetSelect(IRenderedComponent<MudDialogProvider> provider)
  {
    return provider.FindComponents<MudSelect<VacancyStage>>().Single(c => c.Instance.Class!.Contains("reopen-target"));
  }

  private async Task<Vacancy> LoadAsync(Guid vacancyId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies.Include(v => v.Application).SingleAsync(v => v.Id == vacancyId);
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId, VacancyStage defaultTarget)
  {
    return ShowDialogAsync(new DialogParameters<ReopenVacancyDialog>
    {
      { d => d.VacancyId, vacancyId },
      { d => d.DefaultTarget, defaultTarget },
    });
  }
}
