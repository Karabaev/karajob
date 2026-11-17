using Bunit;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using Karajob.Web.Components.Vacancies;
using MudBlazor;
using BoardPage = Karajob.Web.Components.Pages.Board;

namespace Karajob.Tests.Web.Components.Pages;

public class BoardPageTests : VacancyWebFixture
{
  [Fact]
  public void Render_ShowsFiveColumnsInStageOrder()
  {
    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() => Assert.Equal(
      ["Targeted", "Preparing", "Applied", "Interviewing", "Closed"],
      cut.FindAll(".board-column").Select(c => c.GetAttribute("data-stage"))));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public async Task Render_VacancyInStage_AppearsInItsColumn(VacancyStage stage)
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(stage, trackId, "https://example.com/jobs/1");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, stage, id)));
  }

  [Fact]
  public async Task Render_InboxAndDismissedVacancies_AreNotShown()
  {
    var trackId = await CreateTrackAsync();
    var inbox = await Vacancies.CreateAsync(Input("https://example.com/jobs/inbox"));
    var dismissed = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/dismissed");
    await Vacancies.DismissAsync(dismissed, new Karajob.Application.Vacancies.DismissInput(DismissReason.Other, null));
    var shown = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/shown");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, shown)));
    Assert.DoesNotContain(cut.FindAll(".board-card"), c => c.GetAttribute("data-id") == inbox.Id.ToString());
    Assert.DoesNotContain(cut.FindAll(".board-card"), c => c.GetAttribute("data-id") == dismissed.ToString());
  }

  [Fact]
  public async Task Render_ClosedVacancies_AreCollapsedAndShowOutcomeCounts()
  {
    var trackId = await CreateTrackAsync();
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/1", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/2", ApplicationOutcome.Rejected);
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/3", ApplicationOutcome.Rejected);

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal("1", Count(cut, ApplicationOutcome.Offer));
      Assert.Equal("2", Count(cut, ApplicationOutcome.Rejected));
      Assert.Equal("0", Count(cut, ApplicationOutcome.Ghosted));
      Assert.Equal("0", Count(cut, ApplicationOutcome.Withdrawn));
    });
    Assert.Empty(CardsIn(cut, VacancyStage.Closed, closed));
  }

  [Fact]
  public async Task ClickClosedToggle_RevealsClosedCards()
  {
    var trackId = await CreateTrackAsync();
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();

    cut.WaitForElement(".board-closed-toggle").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Closed, closed)));
  }

  [Fact]
  public async Task ClickNext_OnTargetedCard_MovesVacancyToPreparing()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));

    CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task ClickNext_OnApplicationCard_MovesVacancyToInterviewing()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Applied, id)));

    CardsIn(cut, VacancyStage.Applied, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Interviewing, id)));
    Assert.Equal(VacancyStage.Interviewing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task ClickNext_OnPreparingCard_OpensApplyDialogAndCancelLeavesStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));

    CardsIn(cut, VacancyStage.Preparing, id).Single().QuerySelector(".board-next")!.Click();
    dialogs.WaitForElement(".apply-submit");
    dialogs.Find(".apply-cancel").Click();

    dialogs.WaitForAssertion(() => Assert.Empty(dialogs.FindAll(".apply-submit")));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
    Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id));
  }

  [Fact]
  public async Task ClickNext_OnPreparingCard_SubmitApplyDialogMovesCardToApplied()
  {
    var trackId = await CreateTrackAsync();
    var (documentId, _) = await CreateCvAsync("Primary", "primary.pdf");
    await SetPrimaryCvAsync(trackId, documentId);
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));
    CardsIn(cut, VacancyStage.Preparing, id).Single().QuerySelector(".board-next")!.Click();
    dialogs.WaitForElement(".apply-submit");

    dialogs.Find(".apply-submit").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Applied, id)));
    Assert.Equal(VacancyStage.Applied, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task ClickNext_OnInterviewingCard_OpensCloseDialogAndSubmitClosesVacancy()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, trackId, "https://example.com/jobs/1");
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Interviewing, id)));
    CardsIn(cut, VacancyStage.Interviewing, id).Single().QuerySelector(".board-next")!.Click();

    dialogs.WaitForElement(".close-submit").Click();

    cut.WaitForAssertion(() => Assert.Empty(CardsIn(cut, VacancyStage.Interviewing, id)));
    Assert.Equal(VacancyStage.Closed, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task Render_StageChangedElsewhereBeforeClick_DoesNotMoveAndKeepsPageUsable()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));
    await Vacancies.MoveAsync(id, VacancyStage.Targeted, VacancyStage.Preparing);

    CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.Empty(CardsIn(cut, VacancyStage.Targeted, id)));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task Drop_PreparingCardOnApplied_OpensApplyDialogAndCancelKeepsCardInPreparing()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId, "https://example.com/jobs/1");
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));

    var drop = DropAsync(cut, id, VacancyStage.Applied);
    dialogs.WaitForElement(".apply-cancel").Click();

    dialogs.WaitForAssertion(() => Assert.Empty(dialogs.FindAll(".apply-submit")));
    await drop;
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));
    Assert.Empty(CardsIn(cut, VacancyStage.Applied, id));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task Drop_TargetedCardOnInterviewing_IsRejectedAndKeepsCardInTargeted()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));
    var eventsBefore = (await Vacancies.FindAsync(id))!.Timeline.Count;

    await DropAsync(cut, id, VacancyStage.Interviewing);

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));
    Assert.Empty(CardsIn(cut, VacancyStage.Interviewing, id));
    var found = (await Vacancies.FindAsync(id))!;
    Assert.Equal(VacancyStage.Targeted, found.Stage);
    Assert.Equal(eventsBefore, found.Timeline.Count);
  }

  [Fact]
  public async Task Drop_TargetedCardOnPreparing_MovesCard()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));

    await DropAsync(cut, id, VacancyStage.Preparing);

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task FilterByTrack_ShowsOnlyMatchingCards()
  {
    var first = await CreateTrackAsync("First");
    var second = await CreateTrackAsync("Second");
    var firstId = await CreateOnBoardAsync(VacancyStage.Targeted, first, "https://example.com/jobs/1");
    var secondId = await CreateOnBoardAsync(VacancyStage.Targeted, second, "https://example.com/jobs/2");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, secondId)));
    var filter = cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class!.Contains("board-filter"));

    await cut.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(first));

    cut.WaitForAssertion(() => Assert.Empty(CardsIn(cut, VacancyStage.Targeted, secondId)));
    Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, firstId));
  }

  [Fact]
  public async Task DismissFromCardMenu_SubmitDialogRemovesCardAndDismissesVacancy()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    var (cut, dialogs) = RenderWithDialogs();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));
    var card = cut.FindComponents<BoardCardView>().Single(c => c.Instance.Card.Id == id);

    var dismiss = cut.InvokeAsync(() => card.Instance.OnDismissRequested.InvokeAsync());
    dialogs.WaitForElement(".dismiss-submit").Click();

    await dismiss;
    cut.WaitForAssertion(() => Assert.Empty(CardsIn(cut, VacancyStage.Targeted, id)));
    Assert.Equal(VacancyStage.Dismissed, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task Render_CardWithTitle_ShowsTitleInCardLink()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");
    await MutateAsync(id, v => v.Title = "Senior Unity Dev");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() =>
    {
      var link = CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector("a")!;
      Assert.Contains("Senior Unity Dev", link.TextContent);
    });
  }

  [Fact]
  public async Task Render_CardWithoutTitleAndCompany_ShowsUrlInCardLink()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/1");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() =>
    {
      var link = CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector("a")!;
      Assert.Contains("https://example.com/jobs/1", link.TextContent);
    });
  }

  [Fact]
  public async Task Render_CardWithoutTitleButWithCompanyAndRole_ShowsCompanyAndRoleInCardLink()
  {
    var trackId = await CreateTrackAsync();
    var company = await Companies.CreateAsync(new Karajob.Application.Companies.CompanyInput("pixel-forge", "Pixel Forge", null, null, [], [], []));
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", company.Id, Karajob.Infrastructure.Persistence.SeedData.UnityDeveloperRoleId, [trackId]));
    await MutateAsync(vacancy.Id, v => v.MoveTo(VacancyStage.Targeted, vacancy.DiscoveredAt.AddMinutes(1)));

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() =>
    {
      var link = CardsIn(cut, VacancyStage.Targeted, vacancy.Id).Single().QuerySelector("a")!;
      Assert.Contains("Pixel Forge", link.TextContent);
      Assert.DoesNotContain("https://example.com/jobs/1", link.TextContent);
    });
  }

  private static async Task DropAsync(IRenderedComponent<BoardPage> cut, Guid id, VacancyStage zone)
  {
    var container = cut.FindComponent<MudDropContainer<BoardCard>>();
    var card = container.Instance.Items!.Single(c => c.Id == id);
    await cut.InvokeAsync(() => container.Instance.ItemDropped.InvokeAsync(new MudItemDropInfo<BoardCard>(card, zone.ToString(), 0)));
  }

  private (IRenderedComponent<BoardPage> Page,IRenderedComponent<MudDialogProvider> Dialogs) RenderWithDialogs()
  {
    Context.Render<MudPopoverProvider>();
    var dialogs = Context.Render<MudDialogProvider>();
    return (Context.Render<BoardPage>(), dialogs);
  }

  private static IReadOnlyList<AngleSharp.Dom.IElement> CardsIn(IRenderedComponent<BoardPage> cut, VacancyStage stage, Guid id)
  {
    return cut.FindAll($".board-column[data-stage='{stage}'] .board-card[data-id='{id}']");
  }

  private static string Count(IRenderedComponent<BoardPage> cut, ApplicationOutcome outcome)
  {
    return cut.Find($".board-outcome-count[data-outcome='{outcome}']").TextContent.Trim();
  }
}
