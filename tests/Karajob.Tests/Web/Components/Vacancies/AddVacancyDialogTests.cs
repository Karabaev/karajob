using Bunit;
using Karajob.Domain.Vacancies;
using Karajob.Web.Components.Vacancies;
using Karajob.Web.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class AddVacancyDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Submit_ValidUrl_CreatesInboxVacancyAndClosesDialog()
  {
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");

    provider.Find(".quick-add-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("https://example.com/jobs/7", item.Url);
  }

  [Fact]
  public async Task Submit_WithNote_StoresNote()
  {
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");
    provider.Find(".quick-add-note textarea").Input("from a friend");

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    var found = await Vacancies.FindAsync(item.Id);
    Assert.Equal("from a friend", found!.Notes);
  }

  [Fact]
  public async Task Submit_ValidUrl_RaisesInboxChangedAndShowsSnackbar()
  {
    var changed = 0;
    Context.Services.GetRequiredService<InboxNotifier>().Changed += () => changed++;
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal(1, changed);
    Assert.NotEmpty(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
  }

  [Fact]
  public async Task Submit_InvalidUrl_ShowsErrorKeepsDialogOpenAndCreatesNothing()
  {
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("not a url");

    provider.Find(".quick-add-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".quick-add-url .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Empty(await Vacancies.ListAsync(VacancyStage.Inbox));
  }

  [Fact]
  public async Task Submit_DuplicateUrl_ClosesDialogAndNavigatesToExistingVacancy()
  {
    var existing = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://www.example.com/jobs/1?utm_source=x");

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var navigation = Context.Services.GetRequiredService<NavigationManager>();
    Assert.EndsWith($"/vacancies/{existing.Id}", navigation.Uri, StringComparison.OrdinalIgnoreCase);
    Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
  }
}
