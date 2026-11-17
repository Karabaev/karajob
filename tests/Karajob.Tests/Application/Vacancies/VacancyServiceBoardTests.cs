using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceBoardTests : VacancyServiceFixture
{
  [Fact]
  public async Task GetBoardAsync_MixedStages_ReturnsOnlyBoardStages()
  {
    var trackId = await CreateTrackAsync();
    foreach (var (stage, i) in BoardMoves.Columns.Select((s, i) => (s, i)))
    {
      await CreateOnBoardAsync(stage, trackId, $"https://example.com/jobs/board-{i}");
    }

    await CreateOnBoardAsync(VacancyStage.Inbox, trackId, "https://example.com/jobs/inbox");
    var dismissed = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/dismissed");
    await Service.DismissAsync(dismissed, new DismissInput(DismissReason.Other, null));

    var board = await Service.GetBoardAsync(null);

    Assert.Equal(BoardMoves.Columns.OrderBy(s => s), board.Cards.Select(c => c.Stage).OrderBy(s => s));
  }

  [Fact]
  public async Task GetBoardAsync_Cards_AreOrderedByDiscoveredAtDescending()
  {
    var trackId = await CreateTrackAsync();
    Clock.UtcNow = Now;
    var older = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/older");
    Clock.UtcNow = Now.AddDays(2);
    var newer = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/newer");
    Clock.UtcNow = Now.AddDays(1);
    var middle = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/middle");

    var board = await Service.GetBoardAsync(null);

    Assert.Equal([newer, middle, older], board.Cards.Select(c => c.Id));
  }

  [Fact]
  public async Task GetBoardAsync_VacancyWithCompanyAndRole_ResolvesNames()
  {
    var trackId = await CreateTrackAsync();
    var companyId = await CreateCompanyAsync("pixel-forge");
    var vacancy = await Service.CreateAsync(Input(
      companyId: companyId,
      roleId: SeedData.UnityDeveloperRoleId,
      trackIds: [trackId]));
    await Service.MoveToBoardAsync(vacancy.Id, [trackId]);

    var card = Assert.Single((await Service.GetBoardAsync(null)).Cards);

    Assert.Equal("pixel-forge", card.CompanyName);
    Assert.False(string.IsNullOrWhiteSpace(card.RoleName));
    Assert.Equal(DefaultUrl, card.Url);
  }

  [Fact]
  public async Task GetBoardAsync_VacancyWithoutCompanyAndRole_HasNullNames()
  {
    var trackId = await CreateTrackAsync();
    await CreateOnBoardAsync(VacancyStage.Targeted, trackId);

    var card = Assert.Single((await Service.GetBoardAsync(null)).Cards);

    Assert.Null(card.CompanyName);
    Assert.Null(card.RoleName);
    Assert.Null(card.Outcome);
  }

  [Fact]
  public async Task GetBoardAsync_ClosedVacancy_CardCarriesOutcome()
  {
    var trackId = await CreateTrackAsync();
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, outcome: ApplicationOutcome.Offer);

    var card = Assert.Single((await Service.GetBoardAsync(null)).Cards);

    Assert.Equal(VacancyStage.Closed, card.Stage);
    Assert.Equal(ApplicationOutcome.Offer, card.Outcome);
  }

  [Fact]
  public async Task GetBoardAsync_ClosedVacancies_CountsOutcomesIncludingZeros()
  {
    var trackId = await CreateTrackAsync();
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/1", ApplicationOutcome.Rejected);
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/2", ApplicationOutcome.Rejected);
    await CreateOnBoardAsync(VacancyStage.Closed, trackId, "https://example.com/jobs/3", ApplicationOutcome.Offer);

    var board = await Service.GetBoardAsync(null);

    Assert.Equal(4, board.ClosedOutcomes.Count);
    Assert.Equal(1, board.ClosedOutcomes[ApplicationOutcome.Offer]);
    Assert.Equal(2, board.ClosedOutcomes[ApplicationOutcome.Rejected]);
    Assert.Equal(0, board.ClosedOutcomes[ApplicationOutcome.Ghosted]);
    Assert.Equal(0, board.ClosedOutcomes[ApplicationOutcome.Withdrawn]);
  }

  [Fact]
  public async Task GetBoardAsync_EmptyBoard_ReturnsNoCardsAndZeroCounts()
  {
    var board = await Service.GetBoardAsync(null);

    Assert.Empty(board.Cards);
    Assert.Equal(4, board.ClosedOutcomes.Count);
    Assert.All(board.ClosedOutcomes.Values, count => Assert.Equal(0, count));
  }

  [Fact]
  public async Task GetBoardAsync_TrackFilter_ReturnsOnlyMatchedVacanciesAcrossStages()
  {
    var wanted = await CreateTrackAsync("Wanted");
    var other = await CreateTrackAsync("Other");
    var targeted = await CreateOnBoardAsync(VacancyStage.Targeted, wanted, "https://example.com/jobs/1");
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, wanted, "https://example.com/jobs/2");
    await CreateOnBoardAsync(VacancyStage.Targeted, other, "https://example.com/jobs/3");

    var board = await Service.GetBoardAsync(wanted);

    Assert.Equal(new[] { closed, targeted }.Order(), board.Cards.Select(c => c.Id).Order());
  }

  [Fact]
  public async Task GetBoardAsync_TrackFilter_CountsOutcomesOnlyForFilteredClosedCards()
  {
    var wanted = await CreateTrackAsync("Wanted");
    var other = await CreateTrackAsync("Other");
    await CreateOnBoardAsync(VacancyStage.Closed, wanted, "https://example.com/jobs/1", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, other, "https://example.com/jobs/2", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, other, "https://example.com/jobs/3", ApplicationOutcome.Ghosted);

    var board = await Service.GetBoardAsync(wanted);

    Assert.Equal(1, board.ClosedOutcomes[ApplicationOutcome.Offer]);
    Assert.Equal(0, board.ClosedOutcomes[ApplicationOutcome.Ghosted]);
  }

  [Fact]
  public async Task GetBoardAsync_OtherUsersVacancies_AreNotVisible()
  {
    var otherUserId = (await PersistenceFixtures.AddUserAsync(Database)).Id;
    var otherTrack = await CreateTrackAsync("Theirs", otherUserId);
    var otherService = ServiceFor(otherUserId);
    var theirs = await otherService.CreateAsync(Input("https://example.com/jobs/theirs", trackIds: [otherTrack]));
    await otherService.MoveToBoardAsync(theirs.Id, [otherTrack]);
    var trackId = await CreateTrackAsync();
    var mine = await CreateOnBoardAsync(VacancyStage.Targeted, trackId, "https://example.com/jobs/mine");

    var board = await Service.GetBoardAsync(null);

    Assert.Equal(mine, Assert.Single(board.Cards).Id);
  }

  [Fact]
  public async Task MoveAsync_TargetedToPreparing_ChangesStageAndAddsStageEvent()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId);
    Clock.UtcNow = Now.AddHours(5);

    await Service.MoveAsync(id, VacancyStage.Targeted, VacancyStage.Preparing);

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Preparing, vacancy.Stage);
    var last = vacancy.Events.OrderBy(e => e.OccurredAt).Last();
    Assert.Equal(ApplicationEventType.StageChanged, last.Type);
    Assert.Equal(VacancyStage.Targeted, last.FromStage);
    Assert.Equal(VacancyStage.Preparing, last.ToStage);
  }

  [Fact]
  public async Task MoveAsync_PreparingBackToTargeted_ChangesStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    await Service.MoveAsync(id, VacancyStage.Preparing, VacancyStage.Targeted);

    Assert.Equal(VacancyStage.Targeted, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveAsync_AppliedToInterviewing_ChangesStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, trackId);

    await Service.MoveAsync(id, VacancyStage.Applied, VacancyStage.Interviewing);

    Assert.Equal(VacancyStage.Interviewing, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveAsync_ClosedToApplied_ReopensAndClearsOutcome()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Closed, trackId, outcome: ApplicationOutcome.Rejected);

    await Service.MoveAsync(id, VacancyStage.Closed, VacancyStage.Applied);

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Applied, vacancy.Stage);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task MoveAsync_ClosedToInterviewing_ReopensAndClearsOutcome()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Closed, trackId, outcome: ApplicationOutcome.Ghosted);

    await Service.MoveAsync(id, VacancyStage.Closed, VacancyStage.Interviewing);

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Interviewing, vacancy.Stage);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task MoveAsync_StaleExpectedStage_ThrowsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.MoveAsync(id, VacancyStage.Targeted, VacancyStage.Preparing));

    Assert.Equal(VacancyStage.Preparing, (await LoadAsync(id)).Stage);
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Applied)]
  [InlineData(VacancyStage.Preparing, VacancyStage.Applied)]
  [InlineData(VacancyStage.Applied, VacancyStage.Closed)]
  [InlineData(VacancyStage.Targeted, VacancyStage.Interviewing)]
  [InlineData(VacancyStage.Targeted, VacancyStage.Targeted)]
  [InlineData(VacancyStage.Targeted, VacancyStage.Dismissed)]
  public async Task MoveAsync_NonDirectMove_ThrowsValidationAndAddsNoEvent(VacancyStage from, VacancyStage to)
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(from, trackId);
    var eventsBefore = (await LoadAsync(id)).Events.Count;

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveAsync(id, from, to));

    var vacancy = await LoadAsync(id);
    Assert.Equal(from, vacancy.Stage);
    Assert.Equal(eventsBefore, vacancy.Events.Count);
  }

  [Fact]
  public async Task MoveAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Service.MoveAsync(Guid.NewGuid(), VacancyStage.Targeted, VacancyStage.Preparing));
  }

  [Fact]
  public async Task GetApplyOptionsAsync_Vacancy_ListsOnlyMatchedTracks()
  {
    var matched = await CreateTrackAsync("Matched");
    await CreateTrackAsync("Unmatched");
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, matched);

    var options = await Service.GetApplyOptionsAsync(id);

    var option = Assert.Single(options.Tracks);
    Assert.Equal(matched, option.Id);
    Assert.Equal("Matched", option.Name);
  }

  [Fact]
  public async Task GetApplyOptionsAsync_TrackWithPrimaryCv_DefaultsToNewestVersion()
  {
    var trackId = await CreateTrackAsync();
    var (documentId, _) = await CreateCvAsync("Primary", "v1.pdf");
    Clock.UtcNow = Now.AddDays(1);
    var newest = await AddCvVersionAsync(documentId, "v2.pdf");
    await MutateTrackAsync(trackId, h => h.PrimaryCvDocumentId = documentId);
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var options = await Service.GetApplyOptionsAsync(id);

    Assert.Equal(newest, Assert.Single(options.Tracks).DefaultCvVersionId);
  }

  [Fact]
  public async Task GetApplyOptionsAsync_TrackWithoutPrimaryCv_HasNoDefault()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var options = await Service.GetApplyOptionsAsync(id);

    Assert.Null(Assert.Single(options.Tracks).DefaultCvVersionId);
  }

  [Fact]
  public async Task GetApplyOptionsAsync_CvVersions_OrderedByCvNameThenNewestFirst()
  {
    var (beta, _) = await CreateCvAsync("Beta", "beta-1.pdf");
    var (alpha, _) = await CreateCvAsync("Alpha", "alpha-1.pdf");
    Clock.UtcNow = Now.AddDays(1);
    await AddCvVersionAsync(alpha, "alpha-2.pdf");
    await AddCvVersionAsync(beta, "beta-2.pdf");
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var options = await Service.GetApplyOptionsAsync(id);

    Assert.Equal(
      ["alpha-2.pdf", "alpha-1.pdf", "beta-2.pdf", "beta-1.pdf"],
      options.CvVersions.Select(v => v.FileName));
    Assert.Equal(["Alpha", "Alpha", "Beta", "Beta"], options.CvVersions.Select(v => v.CvName));
  }

  [Fact]
  public async Task GetApplyOptionsAsync_OtherUsersCv_IsNotListed()
  {
    var otherUserId = (await PersistenceFixtures.AddUserAsync(Database)).Id;
    await CreateCvAsync("Theirs", "theirs.pdf", otherUserId);
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var options = await Service.GetApplyOptionsAsync(id);

    Assert.Empty(options.CvVersions);
  }

  [Fact]
  public async Task GetApplyOptionsAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.GetApplyOptionsAsync(Guid.NewGuid()));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public async Task ApplyAsync_Valid_CreatesApplicationAndMovesToApplied(VacancyStage from)
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(from, trackId);
    Clock.UtcNow = Now.AddHours(5);
    var appliedAt = Now.AddHours(3);

    await Service.ApplyAsync(id, from, new ApplyInput(trackId, cvVersionId, ApplicationChannel.Referral, appliedAt, "  hello  "));

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Applied, vacancy.Stage);
    var application = vacancy.Application!;
    Assert.Equal(trackId, application.TrackId);
    Assert.Equal(cvVersionId, application.CvVersionId);
    Assert.Equal(ApplicationChannel.Referral, application.Channel);
    Assert.Equal(appliedAt, application.AppliedAt);
    Assert.Equal("hello", application.CoverLetter);
    var applied = Assert.Single(vacancy.Events, e => e.Type == ApplicationEventType.StageChanged && e.ToStage == VacancyStage.Applied);
    Assert.Equal(from, applied.FromStage);
  }

  [Fact]
  public async Task ApplyAsync_BlankCoverLetter_StoresNull()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    await Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, coverLetter: "   "));

    Assert.Null((await LoadAsync(id)).Application!.CoverLetter);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtEqualToNow_IsAccepted()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    await Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: Now));

    Assert.Equal(VacancyStage.Applied, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task ApplyAsync_TrackNotMatchedToVacancy_FailsOnTrackId()
  {
    var matched = await CreateTrackAsync("Matched");
    var unmatched = await CreateTrackAsync("Unmatched");
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, matched);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(unmatched, cvVersionId)));

    Assert.Contains("TrackId", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_UnknownCvVersion_FailsOnCvVersionId()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, Guid.NewGuid())));

    Assert.Contains("CvVersionId", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_OtherUsersCvVersion_FailsOnCvVersionId()
  {
    var otherUserId = (await PersistenceFixtures.AddUserAsync(Database)).Id;
    var (_, foreignVersionId) = await CreateCvAsync("Theirs", "theirs.pdf", otherUserId);
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, foreignVersionId)));

    Assert.Contains("CvVersionId", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtInFuture_FailsOnAppliedAt()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: Now.AddMinutes(1))));

    Assert.Contains("AppliedAt", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtBeforeDiscoveryDay_FailsOnAppliedAt()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var discoveredDay = (await LoadAsync(id)).DiscoveredAt.Date;

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: discoveredDay.AddSeconds(-1))));

    Assert.Contains("AppliedAt", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtOnDiscoveryDayStart_IsAccepted()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var discoveredDay = (await LoadAsync(id)).DiscoveredAt.Date;

    await Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: discoveredDay));

    Assert.Equal(VacancyStage.Applied, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtOnDayBeforeLastTransition_FailsOnAppliedAt()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var lastDay = LastStageChange(await LoadAsync(id)).Date;
    Clock.UtcNow = lastDay.AddDays(3);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: lastDay.AddDays(-1).AddHours(23))));

    Assert.Contains("AppliedAt", exception.Errors.Keys);
    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_AppliedAtEarlierOnLastTransitionDay_UsesLastTransitionTime()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var last = LastStageChange(await LoadAsync(id));
    Clock.UtcNow = last.AddHours(5);

    await Service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: last.Date));

    var vacancy = await LoadAsync(id);
    Assert.Equal(last, vacancy.Application!.AppliedAt);
    var applied = Assert.Single(vacancy.Events, e => e.ToStage == VacancyStage.Applied);
    Assert.Equal(last, applied.OccurredAt);
    var ordered = vacancy.Events.Where(e => e.Type == ApplicationEventType.StageChanged).OrderBy(e => e.OccurredAt).Select(e => e.ToStage);
    Assert.Equal(VacancyStage.Applied, ordered.Last());
  }

  [Fact]
  public async Task ApplyAsync_StaleExpectedStage_ThrowsValidationAndAppliesNothing()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Targeted, Valid(trackId, cvVersionId)));

    await AssertNothingAppliedAsync(id, VacancyStage.Preparing);
  }

  [Fact]
  public async Task ApplyAsync_FromInterviewing_ThrowsValidation()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, trackId);

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.ApplyAsync(id, VacancyStage.Interviewing, Valid(trackId, cvVersionId)));

    Assert.Equal(VacancyStage.Interviewing, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task ApplyAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Service.ApplyAsync(Guid.NewGuid(), VacancyStage.Preparing, Valid(trackId, cvVersionId)));
  }

  [Theory]
  [InlineData(VacancyStage.Applied, ApplicationOutcome.Rejected)]
  [InlineData(VacancyStage.Interviewing, ApplicationOutcome.Offer)]
  [InlineData(VacancyStage.Applied, ApplicationOutcome.Withdrawn)]
  [InlineData(VacancyStage.Interviewing, ApplicationOutcome.Ghosted)]
  public async Task CloseAsync_FromApplicationStage_ClosesWithOutcomeAndStageEvent(VacancyStage from, ApplicationOutcome outcome)
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(from, trackId);
    Clock.UtcNow = Now.AddHours(3);

    await Service.CloseAsync(id, from, outcome);

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.Equal(outcome, vacancy.Application!.Outcome);
    var last = vacancy.Events.Where(e => e.Type == ApplicationEventType.StageChanged).OrderBy(e => e.OccurredAt).Last();
    Assert.Equal(from, last.FromStage);
    Assert.Equal(VacancyStage.Closed, last.ToStage);
  }

  [Fact]
  public async Task CloseAsync_FromTargeted_ThrowsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId);

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CloseAsync(id, VacancyStage.Targeted, ApplicationOutcome.Rejected));

    Assert.Equal(VacancyStage.Targeted, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task CloseAsync_StaleExpectedStage_ThrowsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, trackId);

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CloseAsync(id, VacancyStage.Applied, ApplicationOutcome.Rejected));

    var vacancy = await LoadAsync(id);
    Assert.Equal(VacancyStage.Interviewing, vacancy.Stage);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Fact]
  public async Task CloseAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Service.CloseAsync(Guid.NewGuid(), VacancyStage.Applied, ApplicationOutcome.Rejected));
  }

  private static DateTime LastStageChange(Vacancy vacancy)
  {
    return vacancy.Events.Where(e => e.Type == ApplicationEventType.StageChanged).Max(e => e.OccurredAt);
  }

  private static ApplyInput Valid(Guid trackId, Guid cvVersionId, DateTime? appliedAt = null, string? coverLetter = null)
  {
    return new ApplyInput(trackId, cvVersionId, ApplicationChannel.Website, appliedAt ?? Now.AddHours(-1), coverLetter);
  }

  private async Task AssertNothingAppliedAsync(Guid vacancyId, VacancyStage expectedStage)
  {
    var vacancy = await LoadAsync(vacancyId);
    Assert.Equal(expectedStage, vacancy.Stage);
    Assert.Null(vacancy.Application);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(0, await context.JobApplications.CountAsync());
  }

  [Fact]
  public async Task ApplyAsync_MoscowLocalMidnightOfLastTransitionDay_UsesLastTransitionTime()
  {
    var moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    var service = ServiceFor(SeedData.OwnerId, moscow);
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var last = LastStageChange(await LoadAsync(id));
    Clock.UtcNow = last.AddDays(2);
    var localMidnight = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTimeFromUtc(last, moscow).Date, moscow);

    await service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: localMidnight));

    Assert.Equal(last, (await LoadAsync(id)).Application!.AppliedAt);
  }

  [Fact]
  public async Task ApplyAsync_MoscowDayBeforeLastTransitionDay_FailsOnAppliedAt()
  {
    var moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    var service = ServiceFor(SeedData.OwnerId, moscow);
    var trackId = await CreateTrackAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Preparing, trackId);
    var last = LastStageChange(await LoadAsync(id));
    Clock.UtcNow = last.AddDays(2);
    var localMidnight = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTimeFromUtc(last, moscow).Date, moscow);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => service.ApplyAsync(id, VacancyStage.Preparing, Valid(trackId, cvVersionId, appliedAt: localMidnight.AddSeconds(-1))));

    Assert.Contains("AppliedAt", exception.Errors.Keys);
  }
}
