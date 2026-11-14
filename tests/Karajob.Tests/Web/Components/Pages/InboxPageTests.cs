using Bunit;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using InboxPage = Karajob.Web.Components.Pages.Inbox;

namespace Karajob.Tests.Web.Components.Pages;

public class InboxPageTests : VacancyWebFixture
{
  [Fact]
  public void Render_HasLinkToNewVacancy()
  {
    var cut = Context.Render<InboxPage>();

    var link = cut.WaitForElement(".inbox-add");

    Assert.EndsWith("/vacancies/new", link.GetAttribute("href")!.TrimEnd('/'));
  }

  [Fact]
  public void Render_NoVacancies_ShowsEmptyText()
  {
    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".inbox-empty")));
  }

  [Fact]
  public async Task Render_InboxVacancies_ListsEachWithLinkAndHidesEmptyText()
  {
    var first = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var second = await Vacancies.CreateAsync(Input("https://example.com/jobs/2"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains(cut.FindAll("a"), a => LinksTo(a, first.Id));
      Assert.Contains(cut.FindAll("a"), a => LinksTo(a, second.Id));
      Assert.Empty(cut.FindAll(".inbox-empty"));
    });
  }

  [Fact]
  public async Task Render_NonInboxVacancy_IsNotListed()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var targeted = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", hypothesisIds: [hypothesisId]));
    await MutateAsync(targeted.Id, v => v.MoveTo(VacancyStage.Targeted, targeted.DiscoveredAt.AddHours(1)));
    var inbox = await Vacancies.CreateAsync(Input("https://example.com/jobs/2"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("a"), a => LinksTo(a, inbox.Id)));
    Assert.DoesNotContain(cut.FindAll("a"), a => LinksTo(a, targeted.Id));
  }

  private static bool LinksTo(AngleSharp.Dom.IElement anchor, Guid vacancyId)
  {
    return anchor.GetAttribute("href")!.TrimEnd('/').EndsWith($"/vacancies/{vacancyId}", StringComparison.OrdinalIgnoreCase);
  }
}
