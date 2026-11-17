using Bunit;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Hypotheses;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisPageDecisionTests : HypothesisWebFixture
{
  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public async Task Render_ActiveOrExpanded_ShowsCheckInAndDecisionButtons(HypothesisStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".hypothesis-checkin-button"));
      Assert.Single(cut.FindAll(".hypothesis-decision-button"));
    });
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public async Task Render_DraftPausedOrDisabled_HidesCheckInAndDecisionButtons(HypothesisStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value")));
    Assert.Empty(cut.FindAll(".hypothesis-checkin-button"));
    Assert.Empty(cut.FindAll(".hypothesis-decision-button"));
  }

  [Fact]
  public void Render_NewHypothesis_HidesCheckInAndDecisionButtons()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.WaitForElement(".hypothesis-name input");

    Assert.Empty(cut.FindAll(".hypothesis-checkin-button"));
    Assert.Empty(cut.FindAll(".hypothesis-decision-button"));
  }

  [Fact]
  public async Task Render_ActiveHypothesisWithoutDecisions_ShowsEmptyHistoryWithNoItems()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Active);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".hypothesis-decisions");
    Assert.Empty(cut.FindAll(".hypothesis-decision-item"));
  }

  [Fact]
  public async Task Render_HypothesisWithDecisions_ListsOneItemPerDecision()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(HypothesisDecisionKind.Continue, null, null, null));
    await Decisions.RecordDecisionAsync(id, new DecisionInput(HypothesisDecisionKind.Expand, null, "added Serbia", null));
    await Decisions.RecordDecisionAsync(id, new DecisionInput(HypothesisDecisionKind.Continue, "still fine", null, null));

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".hypothesis-decisions .hypothesis-decision-item").Count));
  }

  [Fact]
  public async Task Render_DecisionWithChanges_ShowsChangesText()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(HypothesisDecisionKind.Expand, null, "added Serbia", null));

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Contains("added Serbia", cut.Find(".hypothesis-decision-item").TextContent));
  }

  [Fact]
  public async Task Render_DisabledHypothesisWithDecision_StillShowsHistory()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(HypothesisDecisionKind.Disable, "Not a personal fit", null, null));

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".hypothesis-decision-item"));
      Assert.Contains("Not a personal fit", cut.Find(".hypothesis-decision-item").TextContent);
    });
  }

  [Fact]
  public async Task Render_AnotherHypothesisDecisions_AreNotListed()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Active);
    var other = await CreateWithStatusAsync("Other", HypothesisStatus.Active);
    await Decisions.RecordDecisionAsync(other, new DecisionInput(HypothesisDecisionKind.Continue, null, null, null));

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".hypothesis-decisions");
    Assert.Empty(cut.FindAll(".hypothesis-decision-item"));
  }

  [Fact]
  public async Task Render_DraftHypothesis_StatusSelectOffersOnlyDraftActivePaused()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Draft);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { HypothesisStatus.Draft, HypothesisStatus.Active, HypothesisStatus.Paused }.Order(),
      StatusOptions(cut).Order()));
  }

  [Fact]
  public void Render_NewHypothesis_StatusSelectOffersOnlyDraftActivePaused()
  {
    var cut = Context.Render<HypothesisPage>();

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { HypothesisStatus.Draft, HypothesisStatus.Active, HypothesisStatus.Paused }.Order(),
      StatusOptions(cut).Order()));
  }

  [Fact]
  public async Task Render_ExpandedHypothesis_StatusSelectAlsoOffersExpanded()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Expanded);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      var options = StatusOptions(cut);
      Assert.Contains(HypothesisStatus.Expanded, options);
      Assert.DoesNotContain(HypothesisStatus.Disabled, options);
      Assert.Equal(4, options.Count);
    });
  }

  [Fact]
  public async Task Render_DisabledHypothesis_StatusSelectAlsoOffersDisabled()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", HypothesisStatus.Disabled);

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      var options = StatusOptions(cut);
      Assert.Contains(HypothesisStatus.Disabled, options);
      Assert.DoesNotContain(HypothesisStatus.Expanded, options);
      Assert.Equal(4, options.Count);
    });
  }

  private async Task<Guid> CreateWithStatusAsync(string name, HypothesisStatus status)
  {
    var created = await Service.CreateAsync(Input(name) with { Status = status is HypothesisStatus.Expanded or HypothesisStatus.Disabled ? HypothesisStatus.Active : status });
    if (status is HypothesisStatus.Expanded or HypothesisStatus.Disabled)
    {
      await using var context = Database.CreateContext(SeedData.OwnerId);
      var stored = await context.Hypotheses.SingleAsync(h => h.Id == created.Id);
      stored.Status = status;
      await context.SaveChangesAsync();
    }

    return created.Id;
  }

  private static List<HypothesisStatus> StatusOptions(IRenderedComponent<HypothesisPage> cut)
  {
    var select = cut.FindComponents<MudSelect<HypothesisStatus>>().Single(c => c.Instance.Class?.Contains("hypothesis-status") == true);
    return select.FindComponents<MudSelectItem<HypothesisStatus>>().Select(i => i.Instance.Value).ToList();
  }
}
