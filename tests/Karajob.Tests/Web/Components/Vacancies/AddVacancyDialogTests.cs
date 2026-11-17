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

  [Fact]
  public async Task UrlEntered_FetchesTitleIntoField()
  {
    TitleReader.Title = "Senior Unity Dev";
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());

    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");

    provider.WaitForAssertion(
      () => Assert.Equal("Senior Unity Dev", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
  }

  [Fact]
  public async Task TitleEditedByHand_UrlChanged_KeepsTypedTitle()
  {
    TitleReader.Title = "First";
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/1");
    provider.WaitForAssertion(
      () => Assert.Equal("First", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    provider.Find(".quick-add-title input").Input("Mine");
    TitleReader.Title = "Second";

    provider.Find(".quick-add-url input").Input("https://example.com/jobs/2");

    await Task.Delay(1000);
    provider.Render();
    Assert.Equal("Mine", provider.Find(".quick-add-title input").GetAttribute("value"));
    Assert.DoesNotContain("https://example.com/jobs/2", TitleReader.RequestedUrls);
  }

  [Fact]
  public async Task Submit_EditedTitle_StoresTypedTitle()
  {
    TitleReader.Title = "From page";
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");
    provider.WaitForAssertion(
      () => Assert.Equal("From page", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    provider.Find(".quick-add-title input").Input("Edited");

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("Edited", item.Title);
  }

  [Fact]
  public async Task Submit_FetchedTitle_StoresItWithoutReadingPageAgain()
  {
    TitleReader.Title = "From page";
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");
    provider.WaitForAssertion(
      () => Assert.Equal("From page", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("From page", item.Title);
    Assert.Single(TitleReader.RequestedUrls);
  }

  [Fact]
  public async Task Submit_LinkChangedToPageWithoutTitle_DoesNotKeepPreviousTitle()
  {
    TitleReader.Title = "First";
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/1");
    provider.WaitForAssertion(
      () => Assert.Equal("First", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    TitleReader.Title = null;

    provider.Find(".quick-add-url input").Input("https://example.com/jobs/2");
    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("https://example.com/jobs/2", item.Url);
    Assert.Null(item.Title);
  }

  [Fact]
  public async Task Submit_EmptyTitle_StoresReaderTitle()
  {
    TitleReader.Title = "From page";
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("https://example.com/jobs/7");
    provider.Find(".quick-add-title input").Input(string.Empty);

    provider.Find(".quick-add-submit").Click();

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("From page", item.Title);
  }

  [Fact]
  public async Task UrlInvalid_NoErrorUntilSubmit()
  {
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input").Input("not a url");

    await Task.Delay(800);

    provider.Render();
    Assert.Empty(provider.FindAll(".quick-add-url .mud-input-error"));
    Assert.Empty(TitleReader.RequestedUrls);
  }
}
