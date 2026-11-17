using Karajob.Application.Common;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisDecisionServiceDecisionTests : HypothesisDecisionServiceFixture
{
  [Fact]
  public async Task RecordDecisionAsync_Continue_StoresDecisionAndKeepsStatus()
  {
    var hypothesisId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    Assert.NotEqual(Guid.Empty, result.Decision.Id);
    Assert.Equal(HypothesisDecisionKind.Continue, result.Decision.Kind);
    Assert.Equal(Now, result.Decision.DecidedAt);
    Assert.Equal(HypothesisStatus.Active, result.Status);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(1, await CountDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task RecordDecisionAsync_ContinueOnExpanded_StaysExpandedAndKeepsPeriod()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, changes: "added Serbia"));
    Clock.UtcNow = Now.AddDays(3);

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    Assert.Equal(HypothesisStatus.Expanded, result.Status);
    Assert.Equal(Now, (await LoadHypothesisAsync(hypothesisId)).PeriodStartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_ReasonAndChanges_AreTrimmedAndReturned()
  {
    var hypothesisId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(
      hypothesisId,
      Decision(HypothesisDecisionKind.Expand, "  market is narrow ", "  added EU locations "));

    Assert.Equal("market is narrow", result.Decision.Reason);
    Assert.Equal("added EU locations", result.Decision.Changes);
  }

  [Fact]
  public async Task RecordDecisionAsync_Snapshot_CapturesPeriodMarketConversionAndWellBeing()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4, conversionThresholdPermille: 30);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 3);
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 2, 2));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 3, 2));
    var review = await Decisions.GetReviewAsync(hypothesisId);

    var decision = (await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue))).Decision;

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
    var hypothesisId = await CreateActiveAsync();

    var decision = (await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue))).Decision;

    Assert.Null(decision.WellBeingTenths);
  }

  [Fact]
  public async Task RecordDecisionAsync_SnapshotIsPersistedAndListed()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 3);
    var recorded = (await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue))).Decision;

    var listed = Assert.Single(await Decisions.ListDecisionsAsync(hypothesisId));

    Assert.Equal(recorded.Id, listed.Id);
    Assert.Equal(3, listed.MarketVacancies);
    Assert.Equal(2, listed.MarketThreshold);
    Assert.Equal(recorded.PeriodStartedAt, listed.PeriodStartedAt);
    Assert.Equal(recorded.Hint, listed.Hint);
  }

  [Fact]
  public async Task RecordDecisionAsync_TwoLowCheckIns_SnapshotHintIsDisable()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(PreviousWeek, 1, 2));
    await Decisions.SaveCheckInAsync(hypothesisId, CheckIn(CurrentWeek, 1, 2));

    var decision = (await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue))).Decision;

    Assert.Equal(DecisionHint.Disable, decision.Hint);
    Assert.Equal(15, decision.WellBeingTenths);
  }

  [Fact]
  public async Task RecordDecisionAsync_InsufficientConversionData_IsAllowedAndKeepsHint()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 1, minSampleSize: 30);
    var hypothesisId = await CreateActiveAsync();
    await CreateMarketVacanciesAsync(hypothesisId, 1);

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    Assert.Equal(DecisionHint.NotEnoughData, result.Decision.Hint);
    Assert.Equal(0, result.Decision.MaturedApplications);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_SetsExpandedAndRestartsPeriodAtNow()
  {
    var hypothesisId = await CreateActiveAsync();

    var result = await Decisions.RecordDecisionAsync(
      hypothesisId,
      Decision(HypothesisDecisionKind.Expand, changes: "added Serbia"));

    Assert.Equal(HypothesisStatus.Expanded, result.Status);
    var stored = await LoadHypothesisAsync(hypothesisId);
    Assert.Equal(HypothesisStatus.Expanded, stored.Status);
    Assert.Equal(Now, stored.PeriodStartedAt);
    Assert.Equal(Now.AddDays(-60), stored.StartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_SnapshotUsesPeriodBeforeTheDecision()
  {
    await UpdateGoalAsync(marketThresholdPerWeek: 2, minSampleSize: 4);
    var hypothesisId = await CreateActiveAsync();
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);

    var decision = (await Decisions.RecordDecisionAsync(
      hypothesisId,
      Decision(HypothesisDecisionKind.Expand, changes: "added Serbia"))).Decision;

    Assert.Equal(Now.AddDays(-60), decision.PeriodStartedAt);
    Assert.Equal(4, decision.MaturedApplications);
    Assert.Equal(1, decision.PositiveApplications);
  }

  [Fact]
  public async Task RecordDecisionAsync_Expand_MetricsPeriodStartsNowAndDropsEarlierApplications()
  {
    await UpdateGoalAsync(minSampleSize: 4);
    var hypothesisId = await CreateActiveAsync();
    await ApplyMaturedAsync(hypothesisId, total: 4, positive: 1);
    Assert.Equal(4, (await Metrics.GetAsync(hypothesisId)).Conversion.Matured);

    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, changes: "added Serbia"));

    var metrics = await Metrics.GetAsync(hypothesisId);
    Assert.Equal(Now, metrics.PeriodStart);
    Assert.Equal(0, metrics.Conversion.Matured);
    Assert.Equal(0, metrics.Conversion.Positive);
  }

  [Fact]
  public async Task RecordDecisionAsync_ExpandAgainFromExpanded_RestartsPeriodAgain()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, changes: "first"));
    Clock.UtcNow = Now.AddDays(20);

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, changes: "second"));

    Assert.Equal(HypothesisStatus.Expanded, result.Status);
    Assert.Equal(Now.AddDays(20), (await LoadHypothesisAsync(hypothesisId)).PeriodStartedAt);
    Assert.Equal(Now, result.Decision.PeriodStartedAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task RecordDecisionAsync_ExpandWithoutChanges_FailsOnChangesAndSavesNothing(string? changes)
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, "reason", changes)));

    Assert.Contains(nameof(DecisionInput.Changes), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task RecordDecisionAsync_Disable_SetsDisabledAndKeepsPeriod()
  {
    var hypothesisId = await CreateActiveAsync();
    await CreateActiveAsync("Other");

    var result = await Decisions.RecordDecisionAsync(
      hypothesisId,
      Decision(HypothesisDecisionKind.Disable, "Not a personal fit"));

    Assert.Equal(HypothesisStatus.Disabled, result.Status);
    Assert.Equal("Not a personal fit", result.Decision.Reason);
    var stored = await LoadHypothesisAsync(hypothesisId);
    Assert.Equal(HypothesisStatus.Disabled, stored.Status);
    Assert.Equal(Now.AddDays(-60), stored.PeriodStartedAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task RecordDecisionAsync_DisableWithoutReason_FailsOnReasonAndSavesNothing(string? reason)
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, reason)));

    Assert.Contains(nameof(DecisionInput.Reason), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableWithDraft_ActivatesDraftAtNow()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    var draftId = await CreateHypothesisAsync("Draft one");

    var result = await Decisions.RecordDecisionAsync(
      hypothesisId,
      Decision(HypothesisDecisionKind.Disable, "Not a personal fit", activateDraftId: draftId));

    Assert.Equal(HypothesisStatus.Disabled, (await LoadHypothesisAsync(hypothesisId)).Status);
    var draft = await LoadHypothesisAsync(draftId);
    Assert.Equal(HypothesisStatus.Active, draft.Status);
    Assert.Equal(Now, draft.StartedAt);
    Assert.Equal(Now, draft.PeriodStartedAt);
    Assert.Equal(2, result.ActiveCount);
    Assert.Equal(ActiveHypothesesStatus.Ok, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableWithoutDraft_DoesNotTouchDrafts()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    var draftId = await CreateHypothesisAsync("Draft one");

    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "Not a personal fit"));

    var draft = await LoadHypothesisAsync(draftId);
    Assert.Equal(HypothesisStatus.Draft, draft.Status);
    Assert.Null(draft.StartedAt);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftThatIsPaused_FailsOnActivateDraftIdAndSavesNothing()
  {
    var hypothesisId = await CreateActiveAsync("A");
    var paused = await CreateHypothesisAsync("Paused one");
    await SetStatusAsync(paused, HypothesisStatus.Paused);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason", activateDraftId: paused)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(HypothesisStatus.Paused, (await LoadHypothesisAsync(paused)).Status);
    Assert.Equal(0, await CountDecisionsAsync());
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftThatIsActive_FailsOnActivateDraftId()
  {
    var hypothesisId = await CreateActiveAsync("A");
    var otherActive = await CreateActiveAsync("B");

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason", activateDraftId: otherActive)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateUnknownDraft_FailsOnActivateDraftIdAndSavesNothing()
  {
    var hypothesisId = await CreateActiveAsync();

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason", activateDraftId: Guid.NewGuid())));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task RecordDecisionAsync_ActivateDraftOfAnotherProfile_FailsOnActivateDraftIdAndLeavesItDraft()
  {
    var hypothesisId = await CreateActiveAsync();
    var other = await CreateProfileAsync();
    var foreignDraft = await CreateHypothesisAsync("Foreign draft", other);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason", activateDraftId: foreignDraft)));

    Assert.Contains(nameof(DecisionInput.ActivateDraftId), exception.Errors.Keys);
    Assert.Equal(HypothesisStatus.Draft, (await LoadHypothesisAsync(foreignDraft, other)).Status);
    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(hypothesisId)).Status);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableOneOfTwoActive_ReportsTooFew()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason"));

    Assert.Equal(1, result.ActiveCount);
    Assert.Equal(ActiveHypothesesStatus.TooFew, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_DisableOneOfThreeActive_ReportsOk()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C");

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Disable, "reason"));

    Assert.Equal(2, result.ActiveCount);
    Assert.Equal(ActiveHypothesesStatus.Ok, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_ContinueWithFourActive_ReportsTooMany()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("B");
    await CreateActiveAsync("C");
    await CreateActiveAsync("D", status: HypothesisStatus.Expanded);

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    Assert.Equal(4, result.ActiveCount);
    Assert.Equal(ActiveHypothesesStatus.TooMany, result.ActiveStatus);
  }

  [Fact]
  public async Task RecordDecisionAsync_ActiveCount_IgnoresPausedDraftDisabledAndForeign()
  {
    var hypothesisId = await CreateActiveAsync("A");
    await CreateActiveAsync("Paused", status: HypothesisStatus.Paused);
    await CreateActiveAsync("Disabled", status: HypothesisStatus.Disabled);
    await CreateHypothesisAsync("Draft");
    var other = await CreateProfileAsync();
    await CreateActiveAsync("Foreign", other);

    var result = await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    Assert.Equal(1, result.ActiveCount);
    Assert.Equal(ActiveHypothesesStatus.TooFew, result.ActiveStatus);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public async Task RecordDecisionAsync_StatusNotAllowed_FailsWithEmptyKeyAndSavesNothing(HypothesisStatus status)
  {
    var hypothesisId = await CreateActiveAsync(status: status);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue)));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Equal(status, (await LoadHypothesisAsync(hypothesisId)).Status);
    Assert.Equal(0, await CountDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task RecordDecisionAsync_UnknownHypothesis_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Decisions.RecordDecisionAsync(Guid.NewGuid(), Decision(HypothesisDecisionKind.Continue)));
  }

  [Fact]
  public async Task RecordDecisionAsync_HypothesisOfAnotherProfile_ThrowsKeyNotFound()
  {
    var other = await CreateProfileAsync();
    var foreign = await CreateActiveAsync("Foreign", other);

    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Decisions.RecordDecisionAsync(foreign, Decision(HypothesisDecisionKind.Continue)));

    Assert.Equal(HypothesisStatus.Active, (await LoadHypothesisAsync(foreign, other)).Status);
  }

  [Fact]
  public async Task ListDecisionsAsync_NoDecisions_ReturnsEmpty()
  {
    var hypothesisId = await CreateActiveAsync();

    Assert.Empty(await Decisions.ListDecisionsAsync(hypothesisId));
  }

  [Fact]
  public async Task ListDecisionsAsync_SeveralDecisions_NewestFirst()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));
    Clock.UtcNow = Now.AddDays(7);
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Expand, changes: "added Serbia"));
    Clock.UtcNow = Now.AddDays(14);
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));

    var list = await Decisions.ListDecisionsAsync(hypothesisId);

    Assert.Equal([Now.AddDays(14), Now.AddDays(7), Now], list.Select(d => d.DecidedAt));
    Assert.Equal(HypothesisDecisionKind.Expand, list[1].Kind);
  }

  [Fact]
  public async Task ListDecisionsAsync_OtherHypothesisDecisions_AreExcluded()
  {
    var first = await CreateActiveAsync("First");
    var second = await CreateActiveAsync("Second");
    await Decisions.RecordDecisionAsync(first, Decision(HypothesisDecisionKind.Continue));
    await Decisions.RecordDecisionAsync(second, Decision(HypothesisDecisionKind.Disable, "reason"));

    var list = await Decisions.ListDecisionsAsync(first);

    Assert.Equal(HypothesisDecisionKind.Continue, Assert.Single(list).Kind);
  }

  [Fact]
  public async Task ListDecisionsAsync_AnotherProfile_SeesNothing()
  {
    var hypothesisId = await CreateActiveAsync();
    await Decisions.RecordDecisionAsync(hypothesisId, Decision(HypothesisDecisionKind.Continue));
    var other = await CreateProfileAsync();

    var visible = await ListOrEmptyAsync(DecisionsFor(other), hypothesisId);

    Assert.Empty(visible);
  }

  private static async Task<IReadOnlyList<DecisionDetails>> ListOrEmptyAsync(HypothesisDecisionService service, Guid hypothesisId)
  {
    try
    {
      return await service.ListDecisionsAsync(hypothesisId);
    }
    catch (KeyNotFoundException)
    {
      return [];
    }
  }
}
