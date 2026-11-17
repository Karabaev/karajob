using Bunit;
using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Tracks;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Tracks;

public class TrackPageDecisionTests : TrackWebFixture
{
  [Theory]
  [InlineData(TrackStatus.Active)]
  [InlineData(TrackStatus.Expanded)]
  public async Task Render_ActiveOrExpanded_ShowsCheckInAndDecisionButtons(TrackStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".track-checkin-button"));
      Assert.Single(cut.FindAll(".track-decision-button"));
    });
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Paused)]
  [InlineData(TrackStatus.Disabled)]
  public async Task Render_DraftPausedOrDisabled_HidesCheckInAndDecisionButtons(TrackStatus status)
  {
    var id = await CreateWithStatusAsync("Gamedev EU", status);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal("Gamedev EU", cut.Find(".track-name input").GetAttribute("value")));
    Assert.Empty(cut.FindAll(".track-checkin-button"));
    Assert.Empty(cut.FindAll(".track-decision-button"));
  }

  [Fact]
  public void Render_NewTrack_HidesCheckInAndDecisionButtons()
  {
    var cut = Context.Render<TrackPage>();
    cut.WaitForElement(".track-name input");

    Assert.Empty(cut.FindAll(".track-checkin-button"));
    Assert.Empty(cut.FindAll(".track-decision-button"));
  }

  [Fact]
  public async Task Render_ActiveTrackWithoutDecisions_ShowsEmptyHistoryWithNoItems()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Active);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".track-decisions");
    Assert.Empty(cut.FindAll(".track-decision-item"));
  }

  [Fact]
  public async Task Render_TrackWithDecisions_ListsOneItemPerDecision()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(TrackDecisionKind.Continue, null, null, null));
    await Decisions.RecordDecisionAsync(id, new DecisionInput(TrackDecisionKind.Expand, null, "added Serbia", null));
    await Decisions.RecordDecisionAsync(id, new DecisionInput(TrackDecisionKind.Continue, "still fine", null, null));

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".track-decisions .track-decision-item").Count));
  }

  [Fact]
  public async Task Render_DecisionWithChanges_ShowsChangesText()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(TrackDecisionKind.Expand, null, "added Serbia", null));

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Contains("added Serbia", cut.Find(".track-decision-item").TextContent));
  }

  [Fact]
  public async Task Render_DisabledTrackWithDecision_StillShowsHistory()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Active);
    await Decisions.RecordDecisionAsync(id, new DecisionInput(TrackDecisionKind.Disable, "Not a personal fit", null, null));

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      Assert.Single(cut.FindAll(".track-decision-item"));
      Assert.Contains("Not a personal fit", cut.Find(".track-decision-item").TextContent);
    });
  }

  [Fact]
  public async Task Render_AnotherTrackDecisions_AreNotListed()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Active);
    var other = await CreateWithStatusAsync("Other", TrackStatus.Active);
    await Decisions.RecordDecisionAsync(other, new DecisionInput(TrackDecisionKind.Continue, null, null, null));

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForElement(".track-decisions");
    Assert.Empty(cut.FindAll(".track-decision-item"));
  }

  [Fact]
  public async Task Render_DraftTrack_StatusSelectOffersOnlyDraftActivePaused()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Draft);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { TrackStatus.Draft, TrackStatus.Active, TrackStatus.Paused }.Order(),
      StatusOptions(cut).Order()));
  }

  [Fact]
  public void Render_NewTrack_StatusSelectOffersOnlyDraftActivePaused()
  {
    var cut = Context.Render<TrackPage>();

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { TrackStatus.Draft, TrackStatus.Active, TrackStatus.Paused }.Order(),
      StatusOptions(cut).Order()));
  }

  [Fact]
  public async Task Render_ExpandedTrack_StatusSelectAlsoOffersExpanded()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Expanded);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      var options = StatusOptions(cut);
      Assert.Contains(TrackStatus.Expanded, options);
      Assert.DoesNotContain(TrackStatus.Disabled, options);
      Assert.Equal(4, options.Count);
    });
  }

  [Fact]
  public async Task Render_DisabledTrack_StatusSelectAlsoOffersDisabled()
  {
    var id = await CreateWithStatusAsync("Gamedev EU", TrackStatus.Disabled);

    var cut = Context.Render<TrackPage>(p => p.Add(c => c.Id, id));

    cut.WaitForAssertion(() =>
    {
      var options = StatusOptions(cut);
      Assert.Contains(TrackStatus.Disabled, options);
      Assert.DoesNotContain(TrackStatus.Expanded, options);
      Assert.Equal(4, options.Count);
    });
  }

  private async Task<Guid> CreateWithStatusAsync(string name, TrackStatus status)
  {
    var created = await Service.CreateAsync(Input(name) with { Status = status is TrackStatus.Expanded or TrackStatus.Disabled ? TrackStatus.Active : status });
    if (status is TrackStatus.Expanded or TrackStatus.Disabled)
    {
      await using var context = Database.CreateContext(SeedData.OwnerId);
      var stored = await context.Tracks.SingleAsync(h => h.Id == created.Id);
      stored.Status = status;
      await context.SaveChangesAsync();
    }

    return created.Id;
  }

  private static List<TrackStatus> StatusOptions(IRenderedComponent<TrackPage> cut)
  {
    var select = cut.FindComponents<MudSelect<TrackStatus>>().Single(c => c.Instance.Class?.Contains("track-status") == true);
    return select.FindComponents<MudSelectItem<TrackStatus>>().Select(i => i.Instance.Value).ToList();
  }
}
