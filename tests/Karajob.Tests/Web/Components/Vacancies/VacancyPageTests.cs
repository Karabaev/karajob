using Bunit;
using Karajob.Application.Companies;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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
