using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class VacancyPageTests : VacancyWebFixture
{
  [Fact]
  public async Task Render_ExistingVacancy_ShowsUrlAndStage()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal(created.Url, cut.Find(".vacancy-url input").GetAttribute("value")));
    Assert.False(string.IsNullOrWhiteSpace(cut.Find(".vacancy-stage").TextContent));
  }

  [Fact]
  public async Task Render_ExistingVacancyWithTitle_PrefillsTitleField()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", title: "Senior Unity Dev"));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal("Senior Unity Dev", cut.Find(".vacancy-title input").GetAttribute("value")));
  }

  [Fact]
  public async Task Save_TitleEditedOnExistingVacancy_PersistsNewTitle()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", title: "Old title"));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal("Old title", cut.Find(".vacancy-title input").GetAttribute("value")));
    cut.Find(".vacancy-title input").Change("New title");

    cut.Find(".vacancy-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var found = await Vacancies.FindAsync(created.Id);
    while (DateTime.UtcNow < deadline && found!.Title != "New title")
    {
      await Task.Delay(50);
      found = await Vacancies.FindAsync(created.Id);
    }
    Assert.Equal("New title", found!.Title);
  }

  [Fact]
  public async Task ClickFetchTitle_ExistingVacancy_FillsTitleFieldFromReader()
  {
    TitleReader.Title = "Fetched title";
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", title: "Old title"));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal("Old title", cut.Find(".vacancy-title input").GetAttribute("value")));
    TitleReader.RequestedUrls.Clear();

    cut.Find(".vacancy-fetch-title").Click();

    cut.WaitForAssertion(() => Assert.Equal("Fetched title", cut.Find(".vacancy-title input").GetAttribute("value")));
    Assert.Equal(["https://example.com/jobs/1"], TitleReader.RequestedUrls);
  }

  [Fact]
  public async Task Render_ExistingVacancy_ShowsOpenLinkToVacancyUrlInNewTab()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() =>
    {
      var link = cut.Find(".vacancy-open-link");
      Assert.Equal(created.Url, link.GetAttribute("href"));
      Assert.Equal("_blank", link.GetAttribute("target"));
      Assert.Contains("noopener", link.GetAttribute("rel"));
    });
  }

  [Fact]
  public async Task Input_AnotherValidUrl_UpdatesOpenLinkHref()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForElement(".vacancy-open-link");

    cut.Find(".vacancy-url input").Input("https://example.org/careers/7");

    cut.WaitForAssertion(() =>
      Assert.Equal("https://example.org/careers/7", cut.Find(".vacancy-open-link").GetAttribute("href")));
  }

  [Theory]
  [InlineData("not a url")]
  [InlineData("javascript:alert(1)")]
  public async Task Input_NonHttpUrl_HidesOpenLink(string value)
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForElement(".vacancy-open-link");

    cut.Find(".vacancy-url input").Input(value);

    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".vacancy-open-link")));
  }

  [Fact]
  public void Render_NewVacancyWithEmptyUrl_HasNoOpenLink()
  {
    var cut = Context.Render<VacancyPage>();

    cut.WaitForElement(".vacancy-url");
    Assert.Empty(cut.FindAll(".vacancy-open-link"));
  }

  [Fact]
  public void Render_UnknownId_ShowsNotFoundHeading()
  {
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, Guid.NewGuid()));

    cut.WaitForAssertion(() => Assert.Contains("Vacancy not found", cut.Markup));
  }

  [Fact]
  public async Task Render_ExistingVacancy_ShowsDiscoveredItemAndTimelineWithDeleteOnlyForManualEvents()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, created.DiscoveredAt.AddHours(1)));
    await Vacancies.AddEventAsync(
      created.Id,
      new VacancyEventInput(ApplicationEventType.Note, created.DiscoveredAt.AddHours(2), "Позвонил рекрутер"));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Позвонил рекрутер", cut.Find(".vacancy-timeline").TextContent);
      Assert.Contains("Discovered", cut.Find(".vacancy-timeline .timeline-discovered").TextContent);
      Assert.Single(cut.FindAll(".vacancy-timeline .timeline-delete"));
      Assert.True(cut.FindAll(".vacancy-timeline .timeline-item").Count >= 2);
      Assert.NotEmpty(cut.FindAll(".timeline-add"));
    });
  }

  [Fact]
  public async Task Render_ExistingVacancyWithSimilarOne_ShowsWarningWithLink()
  {
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    var first = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", company.Id, SeedData.UnityDeveloperRoleId));
    var second = await Vacancies.CreateAsync(Input("https://example.com/jobs/2", company.Id, SeedData.UnityDeveloperRoleId));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, second.Id));

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll(".vacancy-similar a"),
      a => a.GetAttribute("href")!.TrimEnd('/').EndsWith($"/vacancies/{first.Id}", StringComparison.OrdinalIgnoreCase)));
  }

  [Fact]
  public async Task Render_ExistingVacancyWithoutSimilar_HasNoWarning()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForElement(".vacancy-url");
    Assert.Empty(cut.FindAll(".vacancy-similar"));
  }

  [Fact]
  public async Task Save_ValidUrlOnNewPage_CreatesVacancyAndNavigatesToIt()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/42");

    cut.Find(".vacancy-save").Click();

    var created = await WaitForSingleInboxVacancyAsync();
    var navigation = Context.Services.GetRequiredService<NavigationManager>();
    cut.WaitForAssertion(() =>
      Assert.EndsWith($"/vacancies/{created.Id}", navigation.Uri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task Save_InvalidUrlOnNewPage_ShowsErrorAndCreatesNothing()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("not a url");

    cut.Find(".vacancy-save").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".vacancy-url .mud-input-error")));
    Assert.Empty(await Vacancies.ListAsync(VacancyStage.Inbox));
  }

  [Fact]
  public void Render_NewVacancy_ShowsPlatformsMultiSelectWithLabel()
  {
    var cut = Context.Render<VacancyPage>();

    cut.WaitForAssertion(() =>
    {
      var select = PlatformsSelect(cut).Instance;
      Assert.True(select.MultiSelection);
      Assert.Equal("Platforms", select.Label);
    });
  }

  [Fact]
  public async Task Render_ExistingVacancy_ShowsStoredPlatforms()
  {
    var created = await Vacancies.CreateAsync(
      Input("https://example.com/jobs/1") with { PlatformIds = [SeedData.MobilePlatformId, SeedData.WebPlatformId] });

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { SeedData.MobilePlatformId, SeedData.WebPlatformId }.Order(),
      PlatformsSelect(cut).Instance.SelectedValues!.Order()));
  }

  [Fact]
  public async Task Save_PlatformsSelectedOnNewPage_PersistsPlatforms()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/platforms");
    await cut.InvokeAsync(() => PlatformsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.PcPlatformId, SeedData.VrPlatformId]));

    cut.Find(".vacancy-save").Click();

    var created = await WaitForSingleInboxVacancyAsync();
    var found = await Vacancies.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.VrPlatformId }.Order(), found!.PlatformIds.Order());
  }

  [Fact]
  public async Task Save_PlatformsChangedOnExistingVacancy_ReplacesPlatforms()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1") with { PlatformIds = [SeedData.PcPlatformId] });
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal([SeedData.PcPlatformId], PlatformsSelect(cut).Instance.SelectedValues!));
    await cut.InvokeAsync(() => PlatformsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.ConsolesPlatformId]));

    cut.Find(".vacancy-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var found = await Vacancies.FindAsync(created.Id);
    while (DateTime.UtcNow < deadline && !found!.PlatformIds.Contains(SeedData.ConsolesPlatformId))
    {
      await Task.Delay(50);
      found = await Vacancies.FindAsync(created.Id);
    }

    Assert.Equal([SeedData.ConsolesPlatformId], found!.PlatformIds);
  }

  [Fact]
  public void Render_NewVacancy_ShowsSalaryCurrencySelectWithCatalogCodesDefaultingToRub()
  {
    var cut = Context.Render<VacancyPage>();

    cut.WaitForAssertion(() =>
    {
      var select = SalaryCurrencySelect(cut);
      Assert.False(select.Instance.MultiSelection);
      Assert.Equal("RUB", select.Instance.Value);
      var codes = select.FindComponents<MudSelectItem<string>>().Select(i => i.Instance.Value).ToList();
      Assert.Equal(["EUR", "RUB", "USD"], codes.Order(StringComparer.Ordinal));
    });
  }

  [Fact]
  public async Task Render_CurrencyAddedToCatalog_IsListedInSalaryCurrencySelect()
  {
    await Catalogs.CreateAsync(CatalogKind.Currency, new CatalogInput("Фунт стерлингов", "GBP"));

    var cut = Context.Render<VacancyPage>();

    cut.WaitForAssertion(() => Assert.Contains(
      SalaryCurrencySelect(cut).FindComponents<MudSelectItem<string>>(),
      i => i.Instance.Value == "GBP"));
  }

  [Fact]
  public async Task Render_ExistingVacancyWithSalary_ShowsStoredCurrency()
  {
    var created = await Vacancies.CreateAsync(
      Input("https://example.com/jobs/1") with { SalaryMin = 3000, SalaryMax = 5000, SalaryCurrency = "EUR" });

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal("EUR", SalaryCurrencySelect(cut).Instance.Value));
  }

  [Fact]
  public async Task Save_SalaryWithDefaultCurrencyOnNewPage_PersistsRub()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/salary-default");
    await cut.InvokeAsync(() => SalaryMinField(cut).Instance.ValueChanged.InvokeAsync(3000L));

    cut.Find(".vacancy-save").Click();

    var created = await WaitForSingleInboxVacancyAsync();
    var found = await Vacancies.FindAsync(created.Id);
    Assert.Equal(3000, found!.SalaryMin);
    Assert.Equal("RUB", found.SalaryCurrency);
  }

  [Fact]
  public async Task Save_SalaryWithChosenCurrencyOnNewPage_PersistsChosenCode()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/salary-usd");
    await cut.InvokeAsync(() => SalaryMinField(cut).Instance.ValueChanged.InvokeAsync(3000L));
    await cut.InvokeAsync(() => SalaryCurrencySelect(cut).Instance.ValueChanged.InvokeAsync("USD"));

    cut.Find(".vacancy-save").Click();

    var created = await WaitForSingleInboxVacancyAsync();
    var found = await Vacancies.FindAsync(created.Id);
    Assert.Equal("USD", found!.SalaryCurrency);
  }

  [Fact]
  public void Render_NewVacancy_ShowsEnginesMultiSelectWithLabel()
  {
    var cut = Context.Render<VacancyPage>();

    cut.WaitForAssertion(() =>
    {
      var select = EnginesSelect(cut).Instance;
      Assert.True(select.MultiSelection);
      Assert.Equal("Engines", select.Label);
    });
  }

  [Fact]
  public async Task Render_ExistingVacancy_ShowsStoredEngines()
  {
    var created = await Vacancies.CreateAsync(
      Input("https://example.com/jobs/1") with { EngineIds = [SeedData.UnityEngineId, SeedData.GodotEngineId] });

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { SeedData.UnityEngineId, SeedData.GodotEngineId }.Order(),
      EnginesSelect(cut).Instance.SelectedValues!.Order()));
  }

  [Fact]
  public async Task Save_EnginesSelectedOnNewPage_PersistsEngines()
  {
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/engines");
    await cut.InvokeAsync(() => EnginesSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.UnrealEngineId, SeedData.DefoldEngineId]));

    cut.Find(".vacancy-save").Click();

    var created = await WaitForSingleInboxVacancyAsync();
    var found = await Vacancies.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.UnrealEngineId, SeedData.DefoldEngineId }.Order(), found!.EngineIds.Order());
  }

  [Fact]
  public async Task Save_EnginesChangedOnExistingVacancy_ReplacesEngines()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1") with { EngineIds = [SeedData.UnityEngineId] });
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal([SeedData.UnityEngineId], EnginesSelect(cut).Instance.SelectedValues!));
    await cut.InvokeAsync(() => EnginesSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.CocosEngineId]));

    cut.Find(".vacancy-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var found = await Vacancies.FindAsync(created.Id);
    while (DateTime.UtcNow < deadline && !found!.EngineIds.Contains(SeedData.CocosEngineId))
    {
      await Task.Delay(50);
      found = await Vacancies.FindAsync(created.Id);
    }

    Assert.Equal([SeedData.CocosEngineId], found!.EngineIds);
  }

  [Fact]
  public async Task Select_CompanyAndRoleMatchingExistingVacancyOnNewPage_ShowsSimilarWarning()
  {
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    await Vacancies.CreateAsync(Input("https://example.com/jobs/1", company.Id, SeedData.UnityDeveloperRoleId));
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url");

    await SelectCompanyAsync(cut, ToItem(company));
    Assert.Empty(cut.FindAll(".vacancy-similar"));
    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".vacancy-similar")));
  }

  [Fact]
  public async Task AddEvent_NoteFromTimelineDialog_AppearsInTimeline()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForElement(".timeline-add").Click();
    provider.WaitForElement(".event-note textarea, .event-note input").Input("Позвонил рекрутер");

    provider.Find(".event-save").Click();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll(".vacancy-timeline .timeline-item"),
      item => item.TextContent.Contains("Позвонил рекрутер")));
  }

  [Fact]
  public async Task Select_CreateEntryAndCancelDialog_KeepsPreviousCompany()
  {
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    await Vacancies.CreateAsync(Input("https://example.com/jobs/1", company.Id, SeedData.UnityDeveloperRoleId));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input").Input("https://example.com/jobs/2");
    await SelectCompanyAsync(cut, ToItem(company));
    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);
    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".vacancy-similar")));

    var pending = cut.InvokeAsync(() => CompanyAutocomplete(cut).Instance.ValueChanged.InvokeAsync(
      new CompanyListItem(Guid.Empty, "", "+ Create \"Other\"", null, [], false)));
    provider.WaitForElement(".company-name");
    provider.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();
    await pending.WaitAsync(TimeSpan.FromSeconds(5));

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".vacancy-similar")));
    cut.Find(".vacancy-save").Click();
    var saved = await WaitForInboxCountAsync(2);
    Assert.Contains(saved, v => v.CompanyName == "Pixel Forge");
  }

  [Theory]
  [InlineData(VacancyStage.Applied, ApplicationOutcome.Rejected)]
  [InlineData(VacancyStage.Interviewing, ApplicationOutcome.Rejected)]
  [InlineData(VacancyStage.Closed, ApplicationOutcome.Rejected)]
  [InlineData(VacancyStage.Closed, ApplicationOutcome.Offer)]
  public async Task Render_StageWithApplicationAndCoverLetter_ShowsLetterText(VacancyStage stage, ApplicationOutcome outcome)
  {
    var id = await CreateWithLetterAsync(stage, "Hello,\nI am a Unity developer.", outcome);

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      var text = cut.Find(".vacancy-cover-letter .cover-letter-text").TextContent;
      Assert.Contains("Hello,", text);
      Assert.Contains("I am a Unity developer.", text);
      Assert.Empty(cut.FindAll(".cover-letter-empty"));
    });
  }

  [Fact]
  public async Task Render_AppliedWithoutCoverLetter_ShowsPlaceholderAndNoCopyButton()
  {
    var id = await CreateWithLetterAsync(VacancyStage.Applied, null);

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("No cover letter", cut.Find(".vacancy-cover-letter .cover-letter-empty").TextContent);
      Assert.Empty(cut.FindAll(".cover-letter-text"));
      Assert.Empty(cut.FindAll(".cover-letter-copy"));
    });
  }

  [Theory]
  [InlineData(VacancyStage.Inbox)]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public async Task Render_StageBeforeApplied_HasNoCoverLetterCard(VacancyStage stage)
  {
    var id = await CreateWithLetterAsync(stage, null);

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".vacancy-url");
    Assert.Empty(cut.FindAll(".vacancy-cover-letter"));
  }

  [Fact]
  public async Task Copy_ClickCopyButton_WritesLetterToClipboard()
  {
    const string letter = "Hello,\nI am a Unity developer.";
    var id = await CreateWithLetterAsync(VacancyStage.Applied, letter);
    Context.JSInterop.SetupVoid("navigator.clipboard.writeText", letter);
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".cover-letter-copy");
    cut.Find("button.cover-letter-copy, .cover-letter-copy button").Click();

    cut.WaitForAssertion(() =>
    {
      var call = Context.JSInterop.VerifyInvoke("navigator.clipboard.writeText");
      Assert.Equal(letter, Assert.Single(call.Arguments));
    });
  }

  [Fact]
  public async Task Copy_ClipboardSucceeds_ShowsCopiedSnackbar()
  {
    const string letter = "Hello,\nI am a Unity developer.";
    var id = await CreateWithLetterAsync(VacancyStage.Applied, letter);
    Context.JSInterop.SetupVoid("navigator.clipboard.writeText", letter).SetVoidResult();
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".cover-letter-copy");
    cut.Find("button.cover-letter-copy, .cover-letter-copy button").Click();

    cut.WaitForAssertion(() =>
    {
      var snackbar = Assert.Single(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
      Assert.Equal(Severity.Success, snackbar.Severity);
      Assert.Equal("Copied", snackbar.Message?.ToString());
    });
  }

  [Fact]
  public async Task Copy_ClipboardUnavailable_ShowsWarningSnackbar()
  {
    const string letter = "Hello,\nI am a Unity developer.";
    var id = await CreateWithLetterAsync(VacancyStage.Applied, letter);
    Context.JSInterop.SetupVoid("navigator.clipboard.writeText", letter).SetException(new JSException("not allowed"));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".cover-letter-copy");
    cut.Find("button.cover-letter-copy, .cover-letter-copy button").Click();

    cut.WaitForAssertion(() =>
    {
      var snackbar = Assert.Single(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
      Assert.Equal(Severity.Warning, snackbar.Severity);
      Assert.Equal("Could not copy, select the text manually", snackbar.Message?.ToString());
    });
  }

  private async Task<Guid> CreateWithLetterAsync(VacancyStage stage, string? letter, ApplicationOutcome outcome = ApplicationOutcome.Rejected)
  {
    var hypothesisId = await CreateHypothesisAsync();
    if (stage == VacancyStage.Inbox)
    {
      return (await Vacancies.CreateAsync(Input("https://example.com/jobs/letter", hypothesisIds: [hypothesisId]))).Id;
    }

    return await CreateOnBoardAsync(stage, hypothesisId, "https://example.com/jobs/letter", outcome, letter);
  }

  [Fact]
  public async Task Render_ClosedVacancy_ShowsReopenButton()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1");

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".vacancy-reopen")));
  }

  [Theory]
  [InlineData(VacancyStage.Inbox)]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public async Task Render_VacancyNotClosed_HasNoReopenButton(VacancyStage stage)
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = stage == VacancyStage.Inbox
      ? (await Vacancies.CreateAsync(Input("https://example.com/jobs/1"))).Id
      : await CreateOnBoardAsync(stage, hypothesisId, "https://example.com/jobs/1");

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".vacancy-stage");
    Assert.Empty(cut.FindAll(".vacancy-reopen"));
  }

  [Fact]
  public async Task Reopen_ViaDialog_ReloadsPageWithNewStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1");
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, id));
    var closedLabel = cut.WaitForElement(".vacancy-stage").TextContent;
    cut.WaitForElement(".vacancy-reopen").Click();

    provider.WaitForElement(".reopen-submit").Click();

    cut.WaitForAssertion(() =>
    {
      Assert.NotEqual(closedLabel, cut.Find(".vacancy-stage").TextContent);
      Assert.Empty(cut.FindAll(".vacancy-reopen"));
    });
    Assert.NotEqual(VacancyStage.Closed, (await Vacancies.FindAsync(id))!.Stage);
  }

  private static CompanyListItem ToItem(CompanyDetails company)
  {
    return new CompanyListItem(company.Id, company.Slug, company.Name, company.IconUrl, company.Domains, company.IsBlacklisted);
  }

  private static IRenderedComponent<MudAutocomplete<CompanyListItem>> CompanyAutocomplete(IRenderedComponent<VacancyPage> cut)
  {
    return cut.FindComponent<MudAutocomplete<CompanyListItem>>();
  }

  private static async Task SelectCompanyAsync(IRenderedComponent<VacancyPage> cut, CompanyListItem company)
  {
    await cut.InvokeAsync(() => CompanyAutocomplete(cut).Instance.ValueChanged.InvokeAsync(company));
  }

  [Fact]
  public async Task Render_RoleSelected_HypothesesSelectOffersOnlyHypothesesOfThatRole()
  {
    var unity = await CreateHypothesisAsync("Unity one");
    await CreateHypothesisAsync("Unreal one", SeedData.UnrealDeveloperRoleId);
    var cut = Context.Render<VacancyPage>();
    cut.WaitForElement(".vacancy-url input");

    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.Equal([unity], HypothesisOptionIds(cut)));
  }

  [Fact]
  public async Task Render_NoRoleSelected_HypothesesSelectOffersAllHypotheses()
  {
    var unity = await CreateHypothesisAsync("Unity one");
    var unreal = await CreateHypothesisAsync("Unreal one", SeedData.UnrealDeveloperRoleId);

    var cut = Context.Render<VacancyPage>();

    cut.WaitForAssertion(() => Assert.Equal(new[] { unity, unreal }.Order(), HypothesisOptionIds(cut).Order()));
  }

  [Fact]
  public async Task Render_SelectedHypothesisOfAnotherRole_StaysInOptions()
  {
    var unity = await CreateHypothesisAsync("Unity one");
    var unreal = await CreateHypothesisAsync("Unreal one", SeedData.UnrealDeveloperRoleId);
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", roleId: SeedData.UnityDeveloperRoleId, hypothesisIds: [unity]));
    await MutateAsync(created.Id, v => v.RoleId = SeedData.UnrealDeveloperRoleId);

    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal(new[] { unity, unreal }.Order(), HypothesisOptionIds(cut).Order()));
  }

  [Fact]
  public async Task Render_RoleChangedAfterHypothesisSelected_KeepsSelectedHypothesisAndHidesOthersOfOtherRoles()
  {
    var unity = await CreateHypothesisAsync("Unity one");
    var unityTwo = await CreateHypothesisAsync("Unity two");
    await CreateHypothesisAsync("Unreal one", SeedData.UnrealDeveloperRoleId);
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", roleId: SeedData.UnityDeveloperRoleId, hypothesisIds: [unity]));
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForElement(".vacancy-url input");

    await SelectRoleAsync(cut, SeedData.UnrealDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.DoesNotContain(unityTwo, HypothesisOptionIds(cut)));
    Assert.Contains(unity, HypothesisOptionIds(cut));
  }

  private static List<Guid> HypothesisOptionIds(IRenderedComponent<VacancyPage> cut)
  {
    var select = cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class?.Contains("vacancy-hypotheses") == true);
    return select.FindComponents<MudSelectItem<Guid>>().Select(i => i.Instance.Value).ToList();
  }

  private static IRenderedComponent<MudSelect<Guid>> PlatformsSelect(IRenderedComponent<VacancyPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class?.Contains("vacancy-platforms") == true);
  }

  private static IRenderedComponent<MudSelect<Guid>> EnginesSelect(IRenderedComponent<VacancyPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class?.Contains("vacancy-engines") == true);
  }

  private static IRenderedComponent<MudSelect<string>> SalaryCurrencySelect(IRenderedComponent<VacancyPage> cut)
  {
    return cut.FindComponents<MudSelect<string>>().Single(c => c.Instance.Class?.Contains("vacancy-salary-currency") == true);
  }

  private static IRenderedComponent<MudNumericField<long?>> SalaryMinField(IRenderedComponent<VacancyPage> cut)
  {
    return cut.FindComponents<MudNumericField<long?>>().Single(c => c.Instance.Class?.Contains("vacancy-salary-min") == true);
  }

  private static async Task SelectRoleAsync(IRenderedComponent<VacancyPage> cut, Guid roleId)
  {
    var select = cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class!.Contains("vacancy-role"));
    await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(roleId));
  }

  private async Task<IReadOnlyList<VacancyListItem>> WaitForInboxCountAsync(int count)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var items = await Vacancies.ListAsync(VacancyStage.Inbox);
    while (DateTime.UtcNow < deadline && items.Count < count)
    {
      await Task.Delay(50);
      items = await Vacancies.ListAsync(VacancyStage.Inbox);
    }

    return items;
  }

  private async Task<VacancyListItem> WaitForSingleInboxVacancyAsync()
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    IReadOnlyList<VacancyListItem> items = [];
    while (DateTime.UtcNow < deadline && items.Count == 0)
    {
      items = await Vacancies.ListAsync(VacancyStage.Inbox);
      await Task.Delay(50);
    }

    return Assert.Single(items);
  }
}
