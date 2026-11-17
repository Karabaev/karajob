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
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");

    await ClickAsync(provider, ".quick-add-submit");

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("https://example.com/jobs/7", item.Url);
  }

  [Fact]
  public async Task Submit_WithNote_StoresNote()
  {
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");
    await InputAsync(provider, ".quick-add-note textarea", "from a friend");

    await ClickAsync(provider, ".quick-add-submit");

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
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");

    await ClickAsync(provider, ".quick-add-submit");

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal(1, changed);
    Assert.NotEmpty(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
  }

  [Fact]
  public async Task Submit_InvalidUrl_ShowsErrorKeepsDialogOpenAndCreatesNothing()
  {
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "not a url");

    await ClickAsync(provider, ".quick-add-submit");

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".quick-add-url .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Empty(await Vacancies.ListAsync(VacancyStage.Inbox));
  }

  [Fact]
  public async Task Submit_DuplicateUrl_ClosesDialogAndNavigatesToExistingVacancy()
  {
    var existing = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://www.example.com/jobs/1?utm_source=x");

    await ClickAsync(provider, ".quick-add-submit");

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

    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");

    provider.WaitForAssertion(
      () => Assert.Equal("Senior Unity Dev", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
  }

  [Fact]
  public async Task TitleEditedByHand_UrlChanged_KeepsTypedTitle()
  {
    TitleReader.Title = "First";
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/1");
    provider.WaitForAssertion(
      () => Assert.Equal("First", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    await InputAsync(provider, ".quick-add-title input", "Mine");
    TitleReader.Title = "Second";

    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/2");

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
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");
    provider.WaitForAssertion(
      () => Assert.Equal("From page", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    await InputAsync(provider, ".quick-add-title input", "Edited");

    await ClickAsync(provider, ".quick-add-submit");

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("Edited", item.Title);
  }

  [Fact]
  public async Task Submit_FetchedTitle_StoresItWithoutReadingPageAgain()
  {
    TitleReader.Title = "From page";
    var (provider, reference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");
    provider.WaitForAssertion(
      () => Assert.Equal("From page", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));

    await ClickAsync(provider, ".quick-add-submit");

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
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/1");
    provider.WaitForAssertion(
      () => Assert.Equal("First", provider.Find(".quick-add-title input").GetAttribute("value")),
      TimeSpan.FromSeconds(3));
    TitleReader.Title = null;

    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/2");
    await ClickAsync(provider, ".quick-add-submit");

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
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "https://example.com/jobs/7");
    await InputAsync(provider, ".quick-add-title input", string.Empty);

    await ClickAsync(provider, ".quick-add-submit");

    await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("From page", item.Title);
  }

  [Fact]
  public async Task UrlInvalid_NoErrorUntilSubmit()
  {
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await InputAsync(provider, ".quick-add-url input", "not a url");

    await Task.Delay(800);

    provider.Render();
    Assert.Empty(provider.FindAll(".quick-add-url .mud-input-error"));
    Assert.Empty(TitleReader.RequestedUrls);
  }

  [Fact]
  public async Task Open_FocusesUrlInputExplicitly()
  {
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");

    var focused = ExplicitFocusTargets();

    Assert.Contains(ElementReferenceId(provider, ".quick-add-url input"), focused);
  }

  [Fact]
  public async Task Open_DoesNotFocusTitleOrNoteInputs()
  {
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");

    var focused = ExplicitFocusTargets();

    Assert.DoesNotContain(ElementReferenceId(provider, ".quick-add-title input"), focused);
    Assert.DoesNotContain(ElementReferenceId(provider, ".quick-add-note textarea"), focused);
  }

  [Fact]
  public async Task Open_SecondTime_FocusesUrlInputAgain()
  {
    var (provider, firstReference) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");
    await provider.InvokeAsync(() => firstReference.Close());
    provider.WaitForState(() => provider.FindAll(".quick-add-url input").Count == 0);
    var dialogs = Context.Services.GetRequiredService<IDialogService>();

    await provider.InvokeAsync(() => dialogs.ShowAsync<AddVacancyDialog>("Dialog"));
    provider.WaitForElement(".quick-add-url input");

    Assert.Contains(ElementReferenceId(provider, ".quick-add-url input"), ExplicitFocusTargets());
  }

  [Fact]
  public async Task Open_DialogFocusTrapDoesNotTakeFocus()
  {
    var (provider, _) = await ShowDialogAsync(new DialogParameters<AddVacancyDialog>());
    provider.WaitForElement(".quick-add-url input");

    Assert.Equal([ElementReferenceId(provider, ".quick-add-url input")], ExplicitFocusTargets());
  }

  // Keeps only explicit FocusAsync() calls (preventScroll=false), not the dialog's own focus handling.
  private HashSet<string> ExplicitFocusTargets()
  {
    return Context.JSInterop.Invocations
      .Where(i => i.Identifier == "Blazor._internal.domWrapper.focus" && i.Arguments[1] is false)
      .Select(i => ((ElementReference)i.Arguments[0]!).Id)
      .ToHashSet();
  }

  private static string ElementReferenceId(IRenderedComponent<MudDialogProvider> provider, string selector)
  {
    return provider.Find(selector).GetAttribute("blazor:elementreference")!;
  }

  // Find and trigger run on the renderer's dispatcher together: the title fetch re-renders the dialog
  // in the background, and a handler ID read from a stale element no longer exists.
  private static Task InputAsync(IRenderedComponent<MudDialogProvider> provider, string selector, string value)
  {
    return provider.InvokeAsync(() => provider.Find(selector).Input(value));
  }

  private static Task ClickAsync(IRenderedComponent<MudDialogProvider> provider, string selector)
  {
    return provider.InvokeAsync(() => provider.Find(selector).Click());
  }
}
