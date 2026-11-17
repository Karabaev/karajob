using Bunit;
using Karajob.Application.Cvs;
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
  public async Task Submit_WithPrimaryCvOfTrack_AppliesWithNewestVersionAndCloses()
  {
    var trackId = await CreateTrackAsync();
    var (documentId, _) = await CreateCvAsync("Primary", "v1.pdf");
    var newestVersionId = await AddVersionAsync(documentId, "v2.pdf");
    await SetPrimaryCvAsync(trackId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-submit");

    provider.Find(".apply-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var vacancy = await FindWithApplicationAsync(vacancyId);
    Assert.Equal(VacancyStage.Applied, vacancy.Stage);
    Assert.Equal(trackId, vacancy.Application!.TrackId);
    Assert.Equal(newestVersionId, vacancy.Application.CvVersionId);
  }

  [Fact]
  public async Task Submit_WithFutureDate_ShowsErrorAndStaysOpenWithoutApplying()
  {
    var trackId = await CreateTrackAsync();
    var (documentId, _) = await CreateCvAsync();
    await SetPrimaryCvAsync(trackId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
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
    var trackId = await CreateTrackAsync();
    var (documentId, _) = await CreateCvAsync();
    await SetPrimaryCvAsync(trackId, documentId);
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
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
    var trackId = await CreateTrackAsync();
    var vacancyId = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
    var (provider, reference) = await ShowAsync(vacancyId, VacancyStage.Preparing);
    provider.WaitForElement(".apply-cancel").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(result!.Canceled);
    Assert.Equal(VacancyStage.Preparing, (await FindWithApplicationAsync(vacancyId)).Stage);
  }

  [Fact]
  public async Task Render_WithPreferredTrack_PreselectsItAndAppliesWithIt()
  {
    var alpha = await CreateTrackAsync("Alpha");
    var beta = await CreateTrackAsync("Beta");
    var (documentId, versionId) = await CreateCvAsync();
    await SetPrimaryCvAsync(beta, documentId);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", trackIds: [alpha, beta]));
    await Vacancies.MoveToBoardAsync(vacancy.Id, [alpha, beta]);
    await Vacancies.MoveAsync(vacancy.Id, VacancyStage.Targeted, VacancyStage.Preparing);
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<ApplyDialog>
    {
      { d => d.VacancyId, vacancy.Id },
      { d => d.ExpectedStage, VacancyStage.Preparing },
      { d => d.PreferredTrackId, beta },
    });
    provider.WaitForElement(".apply-submit");

    provider.WaitForAssertion(() => Assert.False(provider.Find(".apply-submit").HasAttribute("disabled")));
    provider.Find(".apply-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var applied = await FindWithApplicationAsync(vacancy.Id);
    Assert.Equal(beta, applied.Application!.TrackId);
    Assert.Equal(versionId, applied.Application.CvVersionId);
  }

  private async Task<Guid> AddVersionAsync(Guid documentId, string fileName)
  {
    await Task.Delay(20);
    var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 4, 5, 6 };
    var details = await Cvs.AddVersionAsync(documentId, new CvUpload(fileName, content.Length, () => new MemoryStream(content), null));
    return details.Versions.Single(v => v.FileName == fileName).Id;
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
