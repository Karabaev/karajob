using Bunit;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using Karajob.Web.Vacancies;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
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
    var trackId = await CreateTrackAsync();
    var targeted = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", trackIds: [trackId]));
    await MutateAsync(targeted.Id, v => v.MoveTo(VacancyStage.Targeted, targeted.DiscoveredAt.AddHours(1)));
    var inbox = await Vacancies.CreateAsync(Input("https://example.com/jobs/2"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("a"), a => LinksTo(a, inbox.Id)));
    Assert.DoesNotContain(cut.FindAll("a"), a => LinksTo(a, targeted.Id));
  }

  [Fact]
  public async Task Render_VacancyWithNotes_ShowsNote()
  {
    await Vacancies.QuickAddAsync(new QuickAddInput("https://example.com/jobs/1", "from a friend"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() => Assert.Contains("from a friend", cut.Find(".inbox-note").TextContent));
  }

  [Fact]
  public async Task Render_VacancyWithoutNotes_HasNoNoteElement()
  {
    await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForElement("a[href*='/vacancies/']");
    Assert.Empty(cut.FindAll(".inbox-note"));
  }

  [Fact]
  public async Task NotifyChanged_AfterVacancyAddedElsewhere_ShowsNewRow()
  {
    var cut = Context.Render<InboxPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".inbox-empty")));
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    Context.Services.GetRequiredService<InboxNotifier>().NotifyChanged();

    cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("a"), a => LinksTo(a, created.Id)));
  }

  [Fact]
  public async Task ClickDismiss_ThenSubmit_RemovesRow()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForElement(".inbox-dismiss").Click();

    dialogs.WaitForElement(".dismiss-submit").Click();

    cut.WaitForAssertion(() => Assert.DoesNotContain(cut.FindAll("a"), a => LinksTo(a, created.Id)));
    Assert.Equal(VacancyStage.Dismissed, (await Vacancies.FindAsync(created.Id))!.Stage);
  }

  [Fact]
  public async Task ClickToBoard_CheckTrackAndSubmit_RemovesRow()
  {
    await CreateTrackAsync();
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForElement(".inbox-to-board").Click();
    dialogs.WaitForElement(".move-to-board-track input").Change(true);
    dialogs.WaitForAssertion(() => Assert.False(dialogs.Find(".move-to-board-submit").HasAttribute("disabled")));

    dialogs.Find(".move-to-board-submit").Click();

    cut.WaitForAssertion(() => Assert.DoesNotContain(cut.FindAll("a"), a => LinksTo(a, created.Id)));
    Assert.Equal(VacancyStage.Targeted, (await Vacancies.FindAsync(created.Id))!.Stage);
  }

  [Fact]
  public async Task Render_VacancyWithTitle_ShowsTitleAsLinkText()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", title: "Senior Unity Dev"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() =>
    {
      var link = cut.FindAll("a").Single(a => LinksTo(a, created.Id));
      Assert.Contains("Senior Unity Dev", link.TextContent);
      Assert.DoesNotContain("https://example.com/jobs/1", link.TextContent);
    });
  }

  [Fact]
  public async Task Render_VacancyWithoutTitle_ShowsUrlAsLinkText()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() =>
    {
      var link = cut.FindAll("a").Single(a => LinksTo(a, created.Id));
      Assert.Contains("https://example.com/jobs/1", link.TextContent);
    });
  }

  private (IRenderedComponent<InboxPage> Page, IRenderedComponent<MudDialogProvider> Dialogs) RenderWithDialogs()
  {
    Context.Render<MudPopoverProvider>();
    var dialogs = Context.Render<MudDialogProvider>();
    return (Context.Render<InboxPage>(), dialogs);
  }

  private static bool LinksTo(AngleSharp.Dom.IElement anchor, Guid vacancyId)
  {
    return anchor.GetAttribute("href")!.TrimEnd('/').EndsWith($"/vacancies/{vacancyId}", StringComparison.OrdinalIgnoreCase);
  }
}
