using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceInboxTests : VacancyServiceFixture
{
  [Fact]
  public async Task MoveToBoardAsync_FromInbox_MovesToTargetedWithHumanMatches()
  {
    var first = await CreateTrackAsync("First");
    var second = await CreateTrackAsync("Second");
    var id = await AddInboxVacancyAsync();

    await Service.MoveToBoardAsync(id, [first, second]);

    var stored = await LoadAsync(id);
    Assert.Equal(VacancyStage.Targeted, stored.Stage);
    Assert.Equal(new[] { first, second }.Order(), stored.Matches.Select(m => m.TrackId).Order());
    Assert.All(stored.Matches, m => Assert.Equal(MatchAuthor.Human, m.MatchedBy));
  }

  [Fact]
  public async Task MoveToBoardAsync_FromInbox_WritesStageChangedEvent()
  {
    var trackId = await CreateTrackAsync();
    var id = await AddInboxVacancyAsync();

    await Service.MoveToBoardAsync(id, [trackId]);

    var stored = await LoadAsync(id);
    var stageEvent = Assert.Single(stored.Events);
    Assert.Equal(ApplicationEventType.StageChanged, stageEvent.Type);
    Assert.Equal(VacancyStage.Inbox, stageEvent.FromStage);
    Assert.Equal(VacancyStage.Targeted, stageEvent.ToStage);
  }

  [Fact]
  public async Task MoveToBoardAsync_ExistingMatches_AreReplacedByGivenSet()
  {
    var old = await CreateTrackAsync("Old");
    var kept = await CreateTrackAsync("Kept");
    var added = await CreateTrackAsync("Added");
    var created = await Service.CreateAsync(Input(trackIds: [old, kept]));

    await Service.MoveToBoardAsync(created.Id, [kept, added]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(new[] { kept, added }.Order(), stored.Matches.Select(m => m.TrackId).Order());
  }

  [Fact]
  public async Task MoveToBoardAsync_DismissedInAnotherTab_FailsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var id = await AddInboxVacancyAsync();
    await MutateAsync(id, v => v.Dismiss(DismissReason.WrongRole, null, Now.AddHours(1)));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [trackId]));

    var stored = await LoadAsync(id);
    Assert.Equal(VacancyStage.Dismissed, stored.Stage);
    Assert.Equal(DismissReason.WrongRole, stored.DismissReason);
  }

  [Fact]
  public async Task MoveToBoardAsync_EmptyTracks_FailsWithTrackIdsError()
  {
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, []));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_UnknownTrack_FailsWithTrackIdsError()
  {
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [Guid.NewGuid()]));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_DisabledTrackNotYetMatched_FailsWithTrackIdsError()
  {
    var disabled = await CreateTrackAsync("Off", status: TrackStatus.Disabled);
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [disabled]));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_DisabledTrackAlreadyMatched_Succeeds()
  {
    var trackId = await CreateTrackAsync("Soon off");
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateTrackAsync(trackId, h => h.Status = TrackStatus.Disabled);

    await Service.MoveToBoardAsync(created.Id, [trackId]);

    Assert.Equal(VacancyStage.Targeted, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_AlreadyTargeted_FailsValidation()
  {
    var trackId = await CreateTrackAsync();
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(created.Id, [trackId]));
  }

  [Fact]
  public async Task MoveToBoardAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    var trackId = await CreateTrackAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.MoveToBoardAsync(Guid.NewGuid(), [trackId]));
  }

  [Fact]
  public async Task DismissAsync_FromInbox_MovesToDismissedWithReason()
  {
    var id = await AddInboxVacancyAsync();

    await Service.DismissAsync(id, new DismissInput(DismissReason.WrongSalary, null));

    var stored = await LoadAsync(id);
    Assert.Equal(VacancyStage.Dismissed, stored.Stage);
    Assert.Equal(DismissReason.WrongSalary, stored.DismissReason);
  }

  [Fact]
  public async Task DismissAsync_WithComment_WritesStageChangedEventWithTrimmedNote()
  {
    var id = await AddInboxVacancyAsync();

    await Service.DismissAsync(id, new DismissInput(DismissReason.Other, "  too far  "));

    var stageEvent = Assert.Single((await LoadAsync(id)).Events);
    Assert.Equal(ApplicationEventType.StageChanged, stageEvent.Type);
    Assert.Equal(VacancyStage.Inbox, stageEvent.FromStage);
    Assert.Equal(VacancyStage.Dismissed, stageEvent.ToStage);
    Assert.Equal("too far", stageEvent.Note);
  }

  [Fact]
  public async Task DismissAsync_BlankComment_StoresNullNote()
  {
    var id = await AddInboxVacancyAsync();

    await Service.DismissAsync(id, new DismissInput(DismissReason.WrongRole, "   "));

    var stageEvent = Assert.Single((await LoadAsync(id)).Events);
    Assert.Null(stageEvent.Note);
  }

  [Fact]
  public async Task DismissAsync_FromTargeted_MovesToDismissed()
  {
    var trackId = await CreateTrackAsync();
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    await Service.DismissAsync(created.Id, new DismissInput(DismissReason.WrongStack, null));

    Assert.Equal(VacancyStage.Dismissed, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task DismissAsync_ExpectedInboxButAlreadyTargeted_FailsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.DismissAsync(created.Id, new DismissInput(DismissReason.WrongStack, null), VacancyStage.Inbox));

    Assert.Equal(VacancyStage.Targeted, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task DismissAsync_ExpectedStageMatches_MovesToDismissed()
  {
    var id = await AddInboxVacancyAsync();

    await Service.DismissAsync(id, new DismissInput(DismissReason.WrongStack, null), VacancyStage.Inbox);

    Assert.Equal(VacancyStage.Dismissed, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task DismissAsync_FromApplied_FailsValidationAndKeepsStage()
  {
    var trackId = await CreateTrackAsync();
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);
    await MutateAsync(created.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(trackId, cvVersion.Id, ApplicationChannel.Website, Now.AddHours(1), null);
    });

    await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.DismissAsync(created.Id, new DismissInput(DismissReason.WrongRole, null)));

    Assert.Equal(VacancyStage.Applied, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task DismissAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
      Service.DismissAsync(Guid.NewGuid(), new DismissInput(DismissReason.WrongRole, null)));
  }

  private async Task<Guid> AddInboxVacancyAsync()
  {
    var created = await Service.CreateAsync(Input());
    return created.Id;
  }
}
