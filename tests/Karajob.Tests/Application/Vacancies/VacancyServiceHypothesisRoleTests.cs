using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceHypothesisRoleTests : VacancyServiceFixture
{
  private static readonly Guid VacancyRole = SeedData.UnityDeveloperRoleId;
  private static readonly Guid OtherRole = SeedData.UnrealDeveloperRoleId;

  [Fact]
  public async Task CreateAsync_HypothesisOfAnotherRole_FailsWithHypothesisIdsError()
  {
    var hypothesisId = await CreateHypothesisAsync("Unreal", roleId: OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input(roleId: VacancyRole, hypothesisIds: [hypothesisId])));

    Assert.Contains(nameof(VacancyInput.HypothesisIds), ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_HypothesisOfSameRole_Succeeds()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);

    var details = await Service.CreateAsync(Input(roleId: VacancyRole, hypothesisIds: [hypothesisId]));

    Assert.Equal([hypothesisId], details.HypothesisIds);
  }

  [Fact]
  public async Task CreateAsync_NoRoleWithHypothesesOfAnyRole_Succeeds()
  {
    var unity = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var unreal = await CreateHypothesisAsync("Unreal", roleId: OtherRole);

    var details = await Service.CreateAsync(Input(roleId: null, hypothesisIds: [unity, unreal]));

    Assert.Equal(new[] { unity, unreal }.Order(), details.HypothesisIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_NewlyAddedHypothesisOfAnotherRole_FailsWithHypothesisIdsError()
  {
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));
    var hypothesisId = await CreateHypothesisAsync("Unreal", roleId: OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: VacancyRole, hypothesisIds: [hypothesisId])));

    Assert.Contains(nameof(VacancyInput.HypothesisIds), ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_HypothesisOfSameRole_Succeeds()
  {
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: VacancyRole, hypothesisIds: [hypothesisId]));

    Assert.Equal([hypothesisId], updated.HypothesisIds);
  }

  [Fact]
  public async Task UpdateAsync_NoRoleWithHypothesisOfAnyRole_Succeeds()
  {
    var created = await Service.CreateAsync(Input());
    var hypothesisId = await CreateHypothesisAsync("Unreal", roleId: OtherRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: null, hypothesisIds: [hypothesisId]));

    Assert.Equal([hypothesisId], updated.HypothesisIds);
  }

  [Fact]
  public async Task UpdateAsync_RoleChangedWhileMatchedHypothesisOfOldRoleStaysSelected_Fails()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, hypothesisIds: [hypothesisId]));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: OtherRole, hypothesisIds: [hypothesisId])));

    Assert.Contains(nameof(VacancyInput.HypothesisIds), ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal(VacancyRole, stored.RoleId);
  }

  [Fact]
  public async Task UpdateAsync_ExistingMatchOfAnotherRoleStaysSelected_Fails()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.RoleId = OtherRole);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(roleId: OtherRole, hypothesisIds: [hypothesisId], notes: "kept")));

    Assert.Contains(nameof(VacancyInput.HypothesisIds), ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_ExistingMatchOfAnotherRoleRemoved_Succeeds()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole, hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.RoleId = OtherRole);

    var updated = await Service.UpdateAsync(created.Id, Input(roleId: OtherRole, hypothesisIds: []));

    Assert.Empty(updated.HypothesisIds);
  }

  [Fact]
  public async Task UpdateAsync_AppliedHypothesisOfAnotherRole_StaysSelectedAndSucceeds()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId);
    await MutateAsync(id, v => v.RoleId = OtherRole);

    var updated = await Service.UpdateAsync(id, Input(roleId: OtherRole, hypothesisIds: [hypothesisId], notes: "kept"));

    Assert.Equal([hypothesisId], updated.HypothesisIds);
  }

  [Fact]
  public async Task MoveToBoardAsync_HypothesisOfAnotherRole_FailsWithHypothesisIdsError()
  {
    var hypothesisId = await CreateHypothesisAsync("Unreal", roleId: OtherRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.MoveToBoardAsync(created.Id, [hypothesisId]));

    Assert.Contains(nameof(VacancyInput.HypothesisIds), ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Inbox, stored.Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_HypothesisOfSameRole_Succeeds()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity", roleId: VacancyRole);
    var created = await Service.CreateAsync(Input(roleId: VacancyRole));

    await Service.MoveToBoardAsync(created.Id, [hypothesisId]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Targeted, stored.Stage);
  }

  [Fact]
  public async Task MoveToBoardAsync_VacancyWithoutRole_AcceptsHypothesisOfAnyRole()
  {
    var hypothesisId = await CreateHypothesisAsync("Unreal", roleId: OtherRole);
    var created = await Service.CreateAsync(Input(roleId: null));

    await Service.MoveToBoardAsync(created.Id, [hypothesisId]);

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Karajob.Domain.Vacancies.VacancyStage.Targeted, stored.Stage);
  }
}
