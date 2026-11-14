using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class MoveToBoardDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Render_ListsOnlyNonDisabledHypotheses()
  {
    await CreateHypothesisAsync("Active one");
    var disabled = await CreateHypothesisAsync("Disabled one");
    await DisableAsync(disabled);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var items = provider.WaitForElements(".move-to-board-hypothesis");
    Assert.Single(items);
    Assert.Contains("Active one", items[0].TextContent);
  }

  [Fact]
  public async Task Render_NothingSelected_SubmitIsDisabled()
  {
    await CreateHypothesisAsync();
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    provider.WaitForElement(".move-to-board-hypothesis");
    Assert.True(provider.Find(".move-to-board-submit").HasAttribute("disabled"));
  }

  [Fact]
  public async Task Render_AlreadyMatchedHypothesis_IsPreChecked()
  {
    var matched = await CreateHypothesisAsync("Matched");
    await CreateHypothesisAsync("Other");
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", hypothesisIds: [matched]));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var items = provider.WaitForElements(".move-to-board-hypothesis");
    Assert.True(IsChecked(items.Single(i => i.TextContent.Contains("Matched"))));
    Assert.False(IsChecked(items.Single(i => i.TextContent.Contains("Other"))));
    Assert.False(provider.Find(".move-to-board-submit").HasAttribute("disabled"));
  }

  [Fact]
  public async Task Submit_AfterCheckingHypothesis_MovesVacancyToTargetedAndCloses()
  {
    var hypothesisId = await CreateHypothesisAsync("Pick me");
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowAsync(vacancy.Id);
    provider.WaitForElements(".move-to-board-hypothesis").Single().QuerySelector("input")!.Change(true);
    provider.WaitForAssertion(() => Assert.False(provider.Find(".move-to-board-submit").HasAttribute("disabled")));

    provider.Find(".move-to-board-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var found = await Vacancies.FindAsync(vacancy.Id);
    Assert.Equal(VacancyStage.Targeted, found!.Stage);
    Assert.Equal([hypothesisId], found.HypothesisIds);
  }

  private static bool IsChecked(IElement item)
  {
    return ((IHtmlInputElement)item.QuerySelector("input")!).IsChecked;
  }

  private async Task DisableAsync(Guid hypothesisId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var hypothesis = await context.Hypotheses.SingleAsync(h => h.Id == hypothesisId);
    hypothesis.Status = HypothesisStatus.Disabled;
    await context.SaveChangesAsync();
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId)
  {
    return ShowDialogAsync(new DialogParameters<MoveToBoardDialog> { { d => d.VacancyId, vacancyId } });
  }
}
