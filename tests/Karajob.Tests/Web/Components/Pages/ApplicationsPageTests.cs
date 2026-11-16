using AngleSharp.Dom;
using Bunit;
using Karajob.Application.Companies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using MudBlazor;
using ApplicationsPage = Karajob.Web.Components.Pages.Applications;

namespace Karajob.Tests.Web.Components.Pages;

public class ApplicationsPageTests : VacancyWebFixture
{
  [Fact]
  public void Render_NoApplications_HasNoRows()
  {
    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForElement(".applications-search");
    Assert.Empty(cut.FindAll(".application-row"));
  }

  [Fact]
  public async Task Render_MixedStages_ShowsRowsOnlyForAppliedVacancies()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await Vacancies.CreateAsync(Input("https://example.com/jobs/inbox"));
    var targeted = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/targeted");
    var applied = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/applied");
    var interviewing = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/interviewing");
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/closed");

    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".application-row").Count));
    Assert.NotNull(RowFor(cut, applied));
    Assert.NotNull(RowFor(cut, interviewing));
    Assert.NotNull(RowFor(cut, closed));
    Assert.Null(RowFor(cut, targeted));
  }

  [Fact]
  public async Task Render_Row_LinksToVacancyCard()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");

    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));
  }

  [Fact]
  public async Task Render_Row_ShowsCompanyAndHypothesis()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity Developer RU");
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v => v.CompanyId = company.Id);

    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForAssertion(() =>
    {
      var text = RowFor(cut, id)!.TextContent;
      Assert.Contains("Pixel Forge", text);
      Assert.Contains("Unity Developer RU", text);
    });
  }

  [Fact]
  public async Task Render_ReopenButton_ExistsOnlyOnClosedRows()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var applied = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var interviewing = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/2");
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/3");

    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".application-row").Count));
    Assert.Empty(RowFor(cut, applied)!.QuerySelectorAll(".application-reopen"));
    Assert.Empty(RowFor(cut, interviewing)!.QuerySelectorAll(".application-reopen"));
    Assert.Single(RowFor(cut, closed)!.QuerySelectorAll(".application-reopen"));
  }

  [Fact]
  public async Task FilterByStage_NarrowsRows()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var interviewing = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/2");
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".application-row").Count));

    await cut.InvokeAsync(() => StageSelect(cut).Instance.ValueChanged.InvokeAsync(VacancyStage.Interviewing));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".application-row"));
      Assert.NotNull(RowFor(cut, interviewing));
    });
  }

  [Fact]
  public async Task FilterByOutcome_NarrowsRows()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var offer = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/2", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/3", ApplicationOutcome.Rejected);
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".application-row").Count));

    await cut.InvokeAsync(() => OutcomeSelect(cut).Instance.ValueChanged.InvokeAsync(ApplicationOutcome.Offer));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".application-row"));
      Assert.NotNull(RowFor(cut, offer));
    });
  }

  [Fact]
  public async Task Search_ByCompanyName_NarrowsRows()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var pixel = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    var acme = await Companies.CreateAsync(new CompanyInput("acme", "Acme Corp", null, null, [], []));
    var pixelVacancy = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var acmeVacancy = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/2");
    await MutateAsync(pixelVacancy, v => v.CompanyId = pixel.Id);
    await MutateAsync(acmeVacancy, v => v.CompanyId = acme.Id);
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".application-row").Count));

    await cut.InvokeAsync(() => SearchField(cut).Instance.ValueChanged.InvokeAsync("pixel"));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".application-row"));
      Assert.NotNull(RowFor(cut, pixelVacancy));
    });
  }

  [Fact]
  public async Task CoverLetterToggle_RevealsLetterTextInChildRow()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v => v.Application!.CoverLetter = "I would love to join your team");
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));
    Assert.Empty(cut.FindAll(".application-cover-letter"));

    RowFor(cut, id)!.QuerySelector(".application-cover-toggle")!.Click();

    cut.WaitForAssertion(() => Assert.Contains(
      "I would love to join your team",
      Assert.Single(cut.FindAll(".application-cover-letter")).TextContent));
  }

  [Fact]
  public async Task CoverLetterToggle_ClickedTwice_HidesLetterAgain()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v => v.Application!.CoverLetter = "I would love to join your team");
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));
    RowFor(cut, id)!.QuerySelector(".application-cover-toggle")!.Click();
    cut.WaitForElement(".application-cover-letter");

    RowFor(cut, id)!.QuerySelector(".application-cover-toggle")!.Click();

    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".application-cover-letter")));
  }

  [Fact]
  public async Task Render_RowWithoutCoverLetter_HasNoToggle()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");

    var cut = Context.Render<ApplicationsPage>();

    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));
    Assert.Empty(RowFor(cut, id)!.QuerySelectorAll(".application-cover-toggle"));
  }

  [Fact]
  public async Task ClickReopen_OpensDialogWithClosedFromStageAsDefaultTarget()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v => v.Close(ApplicationOutcome.Rejected, DateTime.UtcNow));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));

    RowFor(cut, id)!.QuerySelector(".application-reopen")!.Click();

    provider.WaitForElement(".reopen-target");
    var select = provider.FindComponents<MudSelect<VacancyStage>>().Single(c => c.Instance.Class!.Contains("reopen-target"));
    Assert.Equal(VacancyStage.Interviewing, select.Instance.Value);
  }

  [Fact]
  public async Task ReopenViaDialog_ReopensVacancyAndRefreshesRow()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1");
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<ApplicationsPage>();
    cut.WaitForAssertion(() => Assert.NotNull(RowFor(cut, id)));
    RowFor(cut, id)!.QuerySelector(".application-reopen")!.Click();

    provider.WaitForElement(".reopen-submit").Click();

    cut.WaitForAssertion(() =>
    {
      Assert.NotNull(RowFor(cut, id));
      Assert.Empty(RowFor(cut, id)!.QuerySelectorAll(".application-reopen"));
    });
    var vacancy = await Vacancies.FindAsync(id);
    Assert.Equal(VacancyStage.Applied, vacancy!.Stage);
  }

  private static IElement? RowFor(IRenderedComponent<ApplicationsPage> cut, Guid vacancyId)
  {
    return cut.FindAll(".application-row").SingleOrDefault(row => row
      .QuerySelectorAll("a")
      .Any(a => a.GetAttribute("href")!.TrimEnd('/').EndsWith($"/vacancies/{vacancyId}", StringComparison.OrdinalIgnoreCase)));
  }

  private static IRenderedComponent<MudSelect<VacancyStage?>> StageSelect(IRenderedComponent<ApplicationsPage> cut)
  {
    return cut.FindComponents<MudSelect<VacancyStage?>>().Single(c => c.Instance.Class!.Contains("applications-stage"));
  }

  private static IRenderedComponent<MudSelect<ApplicationOutcome?>> OutcomeSelect(IRenderedComponent<ApplicationsPage> cut)
  {
    return cut.FindComponents<MudSelect<ApplicationOutcome?>>().Single(c => c.Instance.Class!.Contains("applications-outcome"));
  }

  private static IRenderedComponent<MudTextField<string>> SearchField(IRenderedComponent<ApplicationsPage> cut)
  {
    return cut.FindComponents<MudTextField<string>>().Single(c => c.Instance.Class!.Contains("applications-search"));
  }
}
