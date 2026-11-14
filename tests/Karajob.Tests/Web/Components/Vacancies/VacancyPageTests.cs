using Bunit;
using Karajob.Application.Companies;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
  public void Render_UnknownId_ShowsNotFoundHeading()
  {
    var cut = Context.Render<VacancyPage>(p => p.Add(c => c.Id, Guid.NewGuid()));

    cut.WaitForAssertion(() => Assert.Contains("Вакансия не найдена", cut.Markup));
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
      Assert.Contains("Найдена", cut.Find(".vacancy-timeline .timeline-discovered").TextContent);
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
      new CompanyListItem(Guid.Empty, "", "+ Создать «Other»", null, [], false)));
    provider.WaitForElement(".company-name");
    provider.FindAll("button").First(b => b.TextContent.Contains("Отмена")).Click();
    await pending.WaitAsync(TimeSpan.FromSeconds(5));

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".vacancy-similar")));
    cut.Find(".vacancy-save").Click();
    var saved = await WaitForInboxCountAsync(2);
    Assert.Contains(saved, v => v.CompanyName == "Pixel Forge");
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
