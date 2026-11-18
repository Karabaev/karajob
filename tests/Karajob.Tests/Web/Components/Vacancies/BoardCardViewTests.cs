using Bunit;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Web.Components.Vacancies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class BoardCardViewTests : VacancyWebFixture
{
  private static readonly Guid CardId = Guid.Parse("11111111-1111-1111-1111-111111111111");

  [Theory]
  [InlineData(VacancyStage.Targeted, null)]
  [InlineData(VacancyStage.Preparing, null)]
  [InlineData(VacancyStage.Applied, null)]
  [InlineData(VacancyStage.Interviewing, null)]
  [InlineData(VacancyStage.Closed, ApplicationOutcome.Rejected)]
  public void Render_OpenableUrl_ShowsOpenLinkInEveryStage(VacancyStage stage, ApplicationOutcome? outcome)
  {
    var cut = RenderCard(Card("https://example.com/jobs/1", stage, outcome));

    var link = Assert.Single(cut.FindAll("a.board-open-link"));
    Assert.Equal("https://example.com/jobs/1", link.GetAttribute("href"));
  }

  [Fact]
  public void Render_OpenLink_OpensInNewTabWithoutOpener()
  {
    var cut = RenderCard(Card("https://example.com/jobs/1"));

    var link = cut.Find("a.board-open-link");
    Assert.Equal("_blank", link.GetAttribute("target"));
    var rel = link.GetAttribute("rel");
    Assert.Contains("noopener", rel);
    Assert.Contains("noreferrer", rel);
  }

  [Fact]
  public void Render_OpenLink_HasAccessibleLabel()
  {
    var cut = RenderCard(Card("https://example.com/jobs/1"));

    Assert.Equal("Open the link", cut.Find("a.board-open-link").GetAttribute("aria-label"));
  }

  [Fact]
  public void Render_OpenLink_DoesNotPointToVacancyPage()
  {
    var cut = RenderCard(Card("https://example.com/jobs/1"));

    var href = cut.Find("a.board-open-link").GetAttribute("href");
    Assert.DoesNotContain("/vacancies", href);
  }

  [Fact]
  public void Render_Card_TitleStillLinksToVacancyPage()
  {
    var cut = RenderCard(Card("https://example.com/jobs/1", title: "Unity Developer"));

    var titleLink = Assert.Single(cut.FindAll("a"), a => a.GetAttribute("href") == $"/vacancies/{CardId}");
    Assert.Contains("Unity Developer", titleLink.TextContent);
  }

  [Theory]
  [InlineData("not a url")]
  [InlineData("javascript:alert(1)")]
  [InlineData("ftp://x.com")]
  [InlineData("")]
  public void Render_UrlNotOpenable_HidesOpenLink(string url)
  {
    var cut = RenderCard(Card(url));

    Assert.Empty(cut.FindAll(".board-open-link"));
  }

  [Fact]
  public void Render_UrlNotOpenable_StillShowsTitleLink()
  {
    var cut = RenderCard(Card("not a url", title: "Unity Developer"));

    Assert.Single(cut.FindAll("a"), a => a.GetAttribute("href") == $"/vacancies/{CardId}");
  }

  private IRenderedComponent<BoardCardView> RenderCard(BoardCard card)
  {
    Context.Render<MudPopoverProvider>();
    return Context.Render<BoardCardView>(p => p.Add(c => c.Card, card));
  }

  private static BoardCard Card(
    string url,
    VacancyStage stage = VacancyStage.Targeted,
    ApplicationOutcome? outcome = null,
    string? title = null)
  {
    return new BoardCard(CardId, url, title, null, null, stage, outcome, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }
}
