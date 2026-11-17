using Karajob.Application.Common;
using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;

namespace Karajob.Tests.Application.Tracks;

public class TrackDecisionServiceDecisionTests : TrackDecisionServiceFixture
{
  [Fact]
  public async Task RecordDecisionAsync_Continue_StoresDecisionAndKeepsStatus()
  {
    var trackId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    Assert.NotEqual(Guid.Empty, result.Decision.Id);
    Assert.Equal(TrackDecisionKind.Continue, result.Decision.Kind);
    Assert.Equal(Now, result.Decision.DecidedAt);
    Assert.Equal(TrackStatus.Active, result.Status);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(1, await CountDecisionsAsync(trackId));
  }

  [Fact]
  public async Task RecordDecisionAsync_ContinueOnExpanded_StaysExpandedAndKeepsPeriod()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, changes: "added Serbia"));
    Clock.UtcNow = Now.AddDays(3);

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    Assert.Equal(TrackStatus.Expanded, result.Status);
    Assert.Equal(Now, (await LoadTrackAsync(trackId)).PeriodStartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_ReasonAndChanges_AreTrimmedAndReturned()
  {
    var trackId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(
      trackId,
      Decision(TrackDecisionKind.Expand, "  market is narrow ", "  added EU locations "));

    Assert.Equal("market is narrow", result.Decision.Reason);
    Assert.Equal("added EU locations", result.Decision.Changes);
  }

  [Fact]
  public async Task RecordDecisionAsync_Snapshot_CapturesPeriodMarketConversionAndWellBeing()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4, conversionThresholdPermille: 30);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 3);
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 3, 2));
    var review = await Decisions.GetReviewAsync(trackId);

    var decision = (await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue))).Decision;

    Assert.Equal(Now, decision.DecidedAt);
    Assert.Equal(review.Metrics.PeriodStart, decision.PeriodStartedAt);
    Assert.Equal(Now.AddDays(-60), decision.PeriodStartedAt);
    Assert.Equal(3, decision.MarketVacancies);
    Assert.Equal(2, decision.MarketThreshold);
    Assert.Equal(1, decision.PositiveApplications);
    Assert.Equal(4, decision.MaturedApplications);
    Assert.Equal(30, decision.ConversionThresholdPermille);
    Assert.Equal(25, decision.WellBeingTenths);
    Assert.Equal(DecisionHint.Continue, decision.Hint);
    Assert.Equal(review.Hint, decision.Hint);
  }

  [Fact]
  public async Task RecordDecisionAsync_NoCheckIns_SnapshotWellBeingIsNull()
  {
    var trackId = await CreateActiveAsync();

    var decision = (await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue))).Decision;

    Assert.Null(decision.WellBeingTenths);
  }

  [Fact]
  public async Task RecordDecisionAsync_SnapshotIsPersistedAndListed()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 3);
    var recorded = (await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue))).Decision;

    var listed = Assert.Single(await Decisions.ListDecisionsAsync(trackId));

    Assert.Equal(recorded.Id, listed.Id);
    Assert.Equal(3, listed.MarketVacancies);
    Assert.Equal(2, listed.MarketThreshold);
    Assert.Equal(recorded.PeriodStartedAt, listed.PeriodStartedAt);
    Assert.Equal(recorded.Hint, listed.Hint);
  }

  [Fact]
  public async Task RecordDecisionAsync_TwoLowCheckIns_SnapshotHintIsDisable()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(trackId, CheckIn(PreviousWeek, 1, 2));
    await Decisions.SaveCheckInAsync(trackId, CheckIn(CurrentWeek, 1, 2));

    var decision = (await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue))).Decision;

    Assert.Equal(DecisionHint.Disable, decision.Hint);
    Assert.Equal(15, decision.WellBeingTenths);
  }

  [Fact]
  public async Task RecordDecisionAsync_InsufficientConversionData_IsAllowedAndKeepsHint()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 30);
    var trackId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(trackId, 1);

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    Assert.Equal(DecisionHint.NotEnoughData, result.Decision.Hint);
    Assert.Equal(0, result.Decision.MaturedApplications);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_SetsExpandedAndRestartsPeriodAtNow()
  {
    var trackId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(
      trackId,
      Decision(TrackDecisionKind.Expand, changes: "added Serbia"));

    Assert.Equal(TrackStatus.Expanded, result.Status);
    var stored = await LoadTrackAsync(trackId);
    Assert.Equal(TrackStatus.Expanded, stored.Status);
    Assert.Equal(Now, stored.PeriodStartedAt);
    Assert.Equal(Now.AddDays(-60), stored.StartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_SnapshotUsesPeriodBeforeTheDecision()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);

    var decision = (await Decisions.RecordDecisionAsync(
      trackId,
      Decision(TrackDecisionKind.Expand, changes: "added Serbia"))).Decision;

    Assert.Equal(Now.AddDays(-60), decision.PeriodStartedAt);
    Assert.Equal(4, decision.MaturedApplications);
    Assert.Equal(1, decision.PositiveApplications);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_MetricsPeriodStartsNowAndDropsEarlierApplications()
  {
    await UpdateGoalAsync(minSampleSize: 4);
    var trackId = await CreateActiveAsync();
    await ApplyMaturedAsync(trackId, total: 4, positive: 1);
    Assert.Equal(4, (await Metrics.GetAsync(trackId)).Conversion.Matured);

    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, changes: "added Serbia"));

    var metrics = await Metrics.GetAsync(trackId);
    Assert.Equal(Now, metrics.PeriodStart);
    Assert.Equal(0, metrics.Conversion.Matured);
    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task RecordDecisionAsync_ExpandAgainFromExpanded_RestartsPeriodAgain()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, changes: "first"));
    Clock.UtcNow = Now.AddDays(20);

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, changes: "second"));

    Assert.Equal(TrackStatus.Expanded, result.Status);
    Assert.Equal(Now.AddDays(20), (await LoadTrackAsync(trackId)).PeriodStartedAt);
    Assert.Equal(Now, result.Decision.PeriodStartedAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task RecordDecisionAsync_ExpandWithoutChanges_FailsOnChangesAndSavesNothing(string? changes)
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, "reason", changes)));

    Assert.Contains(nameof(DecisionInput.Changes), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(trackId));
  }

  [Fact]
  public async Task RecordDecisionAsync_Disable_SetsDisabledAndKeepsPeriod()
  {
    var trackId = await CreateActiveAsync();
    await CreateActiveAsync("Other");

    var result = await Decisions.RecordDecisionAsync(
      trackId,
      Decision(TrackDecisionKind.Disable, "Not a personal fit"));

    Assert.Equal(TrackStatus.Disabled, result.Status);
    Assert.Equal("Not a personal fit", result.Decision.Reason);
    var stored = await LoadTrackAsync(trackId);
    Assert.Equal(TrackStatus.Disabled, stored.Status);
    Assert.Equal(Now.AddDays(-60), stored.PeriodStartedAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task RecordDecisionAsync_DisableWithoutReason_FailsOnReasonAndSavesNothing(string? reason)
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, reason)));

    Assert.Contains(nameof(DecisionInput.Reason), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(trackId));
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableWithDraft_ActivatesDraftAtNow()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    var draftId = await CreateTrackAsync("Draft one");

    var result = await Decisions.RecordDecisionAsync(
      trackId,
      Decision(TrackDecisionKind.Disable, "Not a personal fit", activateDraftId: draftId));

    Assert.Equal(TrackStatus.Disabled, (await LoadTrackAsync(trackId)).Status);
    var draft = await LoadTrackAsync(draftId);
    Assert.Equal(TrackStatus.Active, draft.Status);
    Assert.Equal(Now, draft.StartedAt);
    Assert.Equal(Now, draft.PeriodStartedAt);
    Assert.Equal(2, result.ActiveCount);
    Assert.Equal(ActiveTracksStatus.Ok, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableWithoutDraft_DoesNotTouchDrafts()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    var draftId = await CreateTrackAsync("Draft one");

    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "Not a personal fit"));

    var draft = await LoadTrackAsync(draftId);
    Assert.Equal(TrackStatus.Draft, draft.Status);
    Assert.Null(draft.StartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftThatIsPaused_FailsOnActivateDraftIdAndSavesNothing()
  {
    var trackId = await CreateActiveAsync("A");
    var paused = await CreateTrackAsync("Paused one");
    await SetStatusAsync(paused, TrackStatus.Paused);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason", activateDraftId: paused)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(TrackStatus.Paused, (await LoadTrackAsync(paused)).Status);
    Assert.Equal(0, await CountDecisionsAsync());
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftThatIsActive_FailsOnActivateDraftId()
  {
    var trackId = await CreateActiveAsync("A");
    var otherActive = await CreateActiveAsync("B");

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason", activateDraftId: otherActive)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateUnknownDraft_FailsOnActivateDraftIdAndSavesNothing()
  {
    var trackId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason", activateDraftId: Guid.NewGuid())));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(trackId));
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftOfAnotherProfile_FailsOnActivateDraftIdAndLeavesItDraft()
  {
    var trackId = await CreateActiveAsync();
    var other = await CreateProfileAsync();
    var foreignDraft = await CreateTrackAsync("Foreign draft", other);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason", activateDraftId: foreignDraft)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(TrackStatus.Draft, (await LoadTrackAsync(foreignDraft, other)).Status);
    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(trackId)).Status);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableOneOfTwoActive_ReportsTooFew()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason"));

    Assert.Equal(1, result.ActiveCount);
    Assert.Equal(ActiveTracksStatus.TooFew, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableOneOfThreeActive_ReportsOk()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C");

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Disable, "reason"));

    Assert.Equal(2, result.ActiveCount);
    Assert.Equal(ActiveTracksStatus.Ok, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_ContinueWithFourActive_ReportsTooMany()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C");
    await CreateActiveAsync("D", status: TrackStatus.Expanded);

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    Assert.Equal(4, result.ActiveCount);
    Assert.Equal(ActiveTracksStatus.TooMany, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActiveCount_IgnoresPausedDraftDisabledAndForeign()
  {
    var trackId = await CreateActiveAsync("A");
    await CreateActiveAsync("Paused", status: TrackStatus.Paused);
    await CreateActiveAsync("Disabled", status: TrackStatus.Disabled);
    await CreateTrackAsync("Draft");
    var other = await CreateProfileAsync();
    await CreateActiveAsync("Foreign", other);

    var result = await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    Assert.Equal(1, result.ActiveCount);
    Assert.Equal(ActiveTracksStatus.TooFew, result.ActiveStatus);
  }

  [Theory]
  [InlineData(TrackStatus.Draft)]
  [InlineData(TrackStatus.Paused)]
  [InlineData(TrackStatus.Disabled)]
  public async Task RecordDecisionAsync_StatusNotAllowed_FailsWithEmptyKeyAndSavesNothing(TrackStatus status)
  {
    var trackId = await CreateActiveAsync(status: status);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue)));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Equal(status, (await LoadTrackAsync(trackId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(trackId));
  }

  [Fact]
  public async Task RecordDecisionAsync_UnknownTrack_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Decisions.RecordDecisionAsync(Guid.NewGuid(), Decision(TrackDecisionKind.Continue)));
  }

  [Fact]
  public async Task RecordDecisionAsync_TrackOfAnotherProfile_ThrowsKeyNotFound()
  {
    var other = await CreateProfileAsync();
    var foreign = await CreateActiveAsync("Foreign", other);

    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Decisions.RecordDecisionAsync(foreign, Decision(TrackDecisionKind.Continue)));

    Assert.Equal(TrackStatus.Active, (await LoadTrackAsync(foreign, other)).Status);
  }

  [Fact]
  public async Task ListDecisionsAsync_NoDecisions_ReturnsEmpty()
  {
    var trackId = await CreateActiveAsync();

    Assert.Empty(await Decisions.ListDecisionsAsync(trackId));
  }

  [Fact]
  public async Task ListDecisionsAsync_SeveralDecisions_NewestFirst()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));
    Clock.UtcNow = Now.AddDays(7);
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Expand, changes: "added Serbia"));
    Clock.UtcNow = Now.AddDays(14);
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));

    var list = await Decisions.ListDecisionsAsync(trackId);

    Assert.Equal([Now.AddDays(14), Now.AddDays(7), Now], list.Select(d => d.DecidedAt));
    Assert.Equal(TrackDecisionKind.Expand, list[1].Kind);
  }

  [Fact]
  public async Task ListDecisionsAsync_OtherTrackDecisions_AreExcluded()
  {
    var first = await CreateActiveAsync("First");
    var second = await CreateActiveAsync("Second");
    await Decisions.RecordDecisionAsync(first, Decision(TrackDecisionKind.Continue));
    await Decisions.RecordDecisionAsync(second, Decision(TrackDecisionKind.Disable, "reason"));

    var list = await Decisions.ListDecisionsAsync(first);

    Assert.Equal(TrackDecisionKind.Continue, Assert.Single(list).Kind);
  }

  [Fact]
  public async Task ListDecisionsAsync_AnotherProfile_SeesNothing()
  {
    var trackId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(trackId, Decision(TrackDecisionKind.Continue));
    var other = await CreateProfileAsync();

    var visible = await ListOrEmptyAsync(DecisionsFor(other), trackId);

    Assert.Empty(visible);
  }

  private static async Task<IReadOnlyList<DecisionDetails>> ListOrEmptyAsync(TrackDecisionService service, Guid trackId)
  {
    try
    {
      return await service.ListDecisionsAsync(trackId);
    }
    catch (KeyNotFoundException)
    {
      return [];
    }
  }
}
