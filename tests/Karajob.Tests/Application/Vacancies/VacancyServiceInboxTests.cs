using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceInboxTests : VacancyServiceFixture
{
  [Fact]
  public async Task MoveToBoardAsync_FromInbox_MovesToTargetedWithHumanMatches()
  {
    var first = await CreateHypothesisAsync("First");
    var second = await CreateHypothesisAsync("Second");
    var id = await AddInboxVacancyAsync();

    await Service.MoveToBoardAsync(id, [first, second]);

    var stored = await LoadAsync(id);
    Assert.Equal(VacancyStage.Targeted, stored.Stage);
    Assert.Equal(new[] { first, second }.Order(), stored.Matches.Select(m => m.HypothesisId).Order());
    Assert.All(stored.Matches, m => Assert.Equal(MatchAuthor.Human, m.MatchedBy));
  }

  [Fact]
  public async Task MoveToBoardAsync_FromInbox_WritesStageChangedEvent()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await AddInboxVacancyAsync();

    await Service.MoveToBoardAsync(id, [hypothesisId]);

    var stored = await LoadAsync(id);
    var stageEvent = Assert.Single(stored.Events);
    Assert.Equal(ApplicationEventType.StageChanged, stageEvent.Type);
    Assert.Equal(VacancyStage.Inbox, stageEvent.FromStage);
    Assert.Equal(VacancyStage.Targeted, stageEvent.ToStage);
  }

  [Fact]
  public async Task MoveToBoardAsync_ExistingMatches_AreReplacedByGivenSet()
  {
    var old = await CreateHypothesisAsync("Old");
    var kept = await CreateHypothesisAsync("Kept");
    var added = await CreateHypothesisAsync("Added");
    var created = await Service.CreateAsync(Input(hypothesisIds: [old, kept]));

    await Service.MoveToBoardAsync(created.Id, [kept, added]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(new[] { kept, added }.Order(), stored.Matches.Select(m => m.HypothesisId).Order());
  }

  [Fact]
  public async Task MoveToBoardAsync_DismissedInAnotherTab_FailsValidationAndKeepsStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await AddInboxVacancyAsync();
    await MutateAsync(id, v => v.Dismiss(DismissReason.WrongRole, null, Now.AddHours(1)));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [hypothesisId]));

    var stored = await LoadAsync(id);
    Assert.Equal(VacancyStage.Dismissed, stored.Stage);
    Assert.Equal(DismissReason.WrongRole, stored.DismissReason);
  }

  [Fact]
  public async Task MoveToBoardAsync_EmptyHypotheses_FailsWithHypothesisIdsError()
  {
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, []));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_UnknownHypothesis_FailsWithHypothesisIdsError()
  {
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [Guid.NewGuid()]));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_DisabledHypothesisNotYetMatched_FailsWithHypothesisIdsError()
  {
    var disabled = await CreateHypothesisAsync("Off", status: HypothesisStatus.Disabled);
    var id = await AddInboxVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(id, [disabled]));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    Assert.Equal(VacancyStage.Inbox, (await LoadAsync(id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_DisabledHypothesisAlreadyMatched_Succeeds()
  {
    var hypothesisId = await CreateHypothesisAsync("Soon off");
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateHypothesisAsync(hypothesisId, h => h.Status = HypothesisStatus.Disabled);

    await Service.MoveToBoardAsync(created.Id, [hypothesisId]);

    Assert.Equal(VacancyStage.Targeted, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_AlreadyTargeted_FailsValidation()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(created.Id, [hypothesisId]));
  }

  [Fact]
  public async Task MoveToBoardAsync_UnknownVacancy_ThrowsKeyNotFound()
  {
    var hypothesisId = await CreateHypothesisAsync();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.MoveToBoardAsync(Guid.NewGuid(), [hypothesisId]));
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
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    await Service.DismissAsync(created.Id, new DismissInput(DismissReason.WrongStack, null));

    Assert.Equal(VacancyStage.Dismissed, (await LoadAsync(created.Id)).Stage);
  }

  [Fact]
  public async Task DismissAsync_ExpectedInboxButAlreadyTargeted_FailsValidationAndKeepsStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
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
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);
    await MutateAsync(created.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(hypothesisId, cvVersion.Id, ApplicationChannel.Website, Now.AddHours(1), null);
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
