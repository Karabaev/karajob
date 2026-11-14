using Bunit;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
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
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(stage, hypothesisId, "https://example.com/jobs/1");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, stage, id)));
  }

  [Fact]
  public async Task Render_InboxAndDismissedVacancies_AreNotShown()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var inbox = await Vacancies.CreateAsync(Input("https://example.com/jobs/inbox"));
    var dismissed = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/dismissed");
    await Vacancies.DismissAsync(dismissed, new Karajob.Application.Vacancies.DismissInput(DismissReason.Other, null));
    var shown = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/shown");

    var cut = Context.Render<BoardPage>();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, shown)));
    Assert.DoesNotContain(cut.FindAll(".board-card"), c => c.GetAttribute("data-id") == inbox.Id.ToString());
    Assert.DoesNotContain(cut.FindAll(".board-card"), c => c.GetAttribute("data-id") == dismissed.ToString());
  }

  [Fact]
  public async Task Render_ClosedVacancies_AreCollapsedAndShowOutcomeCounts()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/2", ApplicationOutcome.Rejected);
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/3", ApplicationOutcome.Rejected);

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
    var hypothesisId = await CreateHypothesisAsync();
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();

    cut.WaitForElement(".board-closed-toggle").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Closed, closed)));
  }

  [Fact]
  public async Task ClickNext_OnTargetedCard_MovesVacancyToPreparing()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));

    CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Preparing, id)));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task ClickNext_OnApplicationCard_MovesVacancyToInterviewing()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Applied, id)));

    CardsIn(cut, VacancyStage.Applied, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Interviewing, id)));
    Assert.Equal(VacancyStage.Interviewing, (await Vacancies.FindAsync(id))!.Stage);
  }

  [Fact]
  public async Task ClickNext_OnPreparingCard_OpensApplyDialogAndCancelLeavesStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
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
    var hypothesisId = await CreateHypothesisAsync();
    var (documentId, _) = await CreateCvAsync("Primary", "primary.pdf");
    await SetPrimaryCvAsync(hypothesisId, documentId);
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/1");
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
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/1");
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
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/1");
    var cut = Context.Render<BoardPage>();
    cut.WaitForAssertion(() => Assert.NotEmpty(CardsIn(cut, VacancyStage.Targeted, id)));
    await Vacancies.MoveAsync(id, VacancyStage.Targeted, VacancyStage.Preparing);

    CardsIn(cut, VacancyStage.Targeted, id).Single().QuerySelector(".board-next")!.Click();

    cut.WaitForAssertion(() => Assert.Empty(CardsIn(cut, VacancyStage.Targeted, id)));
    Assert.Equal(VacancyStage.Preparing, (await Vacancies.FindAsync(id))!.Stage);
  }

  private (IRenderedComponent<BoardPage> Page, IRenderedComponent<MudDialogProvider> Dialogs) RenderWithDialogs()
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
