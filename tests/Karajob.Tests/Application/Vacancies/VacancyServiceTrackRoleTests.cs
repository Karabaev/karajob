using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceTrackRoleTests : VacancyServiceFixture
{
  private static readonly Guid VacancyRole = SeedData.UnityDeveloperRoleId;
  private static readonly Guid OtherRole = SeedData.UnrealDeveloperRoleId;

  [Fact]
  public async Task CreateAsync_TrackOfAnotherRole_FailsWithTrackIdsError()
  {
    var trackId = await CreateTrackAsync("Unreal", roleId: OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input(roleId: VacancyRole, trackIds: [trackId])));

    Assert.Contains(nameof(VacancyInput.TrackIds), ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_TrackOfSameRole_Succeeds()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);

    var details = await Service.CreateAsync(Input(roleId: VacancyRole, trackIds: [trackId]));

    Assert.Equal([trackId], details.TrackIds);
  }

  [Fact]
  public async Task CreateAsync_NoRoleWithTracksOfAnyRole_Succeeds()
  {
    var unity = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var unreal = await CreateTrackAsync("Unreal", roleId: OtherRole);

    var details = await Service.CreateAsync(Input(roleId: null, trackIds: [unity, unreal]));

    Assert.Equal(new[] { unity, unreal }.Order(), details.TrackIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_NewlyAddedTrackOfAnotherRole_FailsWithTrackIdsError()
  {
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));
    var trackId = await CreateTrackAsync("Unreal", roleId: OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: VacancyRole, trackIds: [trackId])));

    Assert.Contains(nameof(VacancyInput.TrackIds), ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_TrackOfSameRole_Succeeds()
  {
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: VacancyRole, trackIds: [trackId]));

    Assert.Equal([trackId], updated.TrackIds);
  }

  [Fact]
  public async Task UpdateAsync_NoRoleWithTrackOfAnyRole_Succeeds()
  {
    var created = await Service.CreateAsync(Input());
    var trackId = await CreateTrackAsync("Unreal", roleId: OtherRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: null, trackIds: [trackId]));

    Assert.Equal([trackId], updated.TrackIds);
  }

  [Fact]
  public async Task UpdateAsync_RoleChangedWhileMatchedTrackOfOldRoleStaysSelected_Fails()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, trackIds: [trackId]));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: OtherRole, trackIds: [trackId])));

    Assert.Contains(nameof(VacancyInput.TrackIds), ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal(VacancyRole, stored.RoleId);
  }

  [Fact]
  public async Task UpdateAsync_ExistingMatchOfAnotherRoleStaysSelected_Fails()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.RoleId = OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: OtherRole, trackIds: [trackId], notes: "kept")));

    Assert.Contains(nameof(VacancyInput.TrackIds), ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_ExistingMatchOfAnotherRoleRemoved_Succeeds()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.RoleId = OtherRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: OtherRole, trackIds: []));

    Assert.Empty(updated.TrackIds);
  }

  [Fact]
  public async Task UpdateAsync_AppliedTrackOfAnotherRole_StaysSelectedAndSucceeds()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var id = await CreateOnBoardAsync(VacancyStage.Applied, trackId);
    await MutateAsync(id, v => v.RoleId = OtherRole);

    var updated = await Service.UpdateAsync(id, Input(roleId: OtherRole, trackIds: [trackId], notes: "kept"));

    Assert.Equal([trackId], updated.TrackIds);
  }

  [Fact]
  public async Task MoveToBoardAsync_TrackOfAnotherRole_FailsWithTrackIdsError()
  {
    var trackId = await CreateTrackAsync("Unreal", roleId: OtherRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(created.Id, [trackId]));

    Assert.Contains(nameof(VacancyInput.TrackIds), ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Inbox, stored.Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_TrackOfSameRole_Succeeds()
  {
    var trackId = await CreateTrackAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));

    await Service.MoveToBoardAsync(created.Id, [trackId]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Targeted, stored.Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_VacancyWithoutRole_AcceptsTrackOfAnyRole()
  {
    var trackId = await CreateTrackAsync("Unreal", roleId: OtherRole);
    var created = await Service.CreateAsync(Input(roleId: null));

    await Service.MoveToBoardAsync(created.Id, [trackId]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Targeted, stored.Stage);
  }
}
