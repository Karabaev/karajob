using Karajob.Application.Common;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisServiceCrudTests : HypothesisServiceFixture
{
  [Fact]
  public async Task CreateAsync_ValidInput_ReturnsAndPersistsDetails()
  {
    var (cv, _) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);

    var created = await Service.CreateAsync(Input(
      locationIds: [SeedData.EuLocationId, SeedData.UsaLocationId],
      industries: ["Games"],
      workFormat: WorkFormat.Remote | WorkFormat.Relocation,
      positioning: "Tools expert",
      primaryCvDocumentId: cv.Id));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal("Unity Developer RU", found.Name);
    Assert.Equal(SeedData.UnityDeveloperRoleId, found.RoleId);
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.UsaLocationId }.Order(), found.LocationIds.Order());
    Assert.Equal(["Games"], found.Industries);
    Assert.Equal(WorkFormat.Remote | WorkFormat.Relocation, found.WorkFormat);
    Assert.Equal(SeedData.RussianLanguageId, found.LanguageId);
    Assert.Equal("Tools expert", found.Positioning);
    Assert.Equal(cv.Id, found.PrimaryCvDocumentId);
  }

  [Fact]
  public async Task CreateAsync_NameWithWhitespace_Trims()
  {
    var created = await Service.CreateAsync(Input(name: "  Name  "));

    Assert.Equal("Name", created.Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task CreateAsync_BlankName_ThrowsValidationWithKey(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(name: name)));

    Assert.Contains(nameof(HypothesisInput.Name), exception.Errors.Keys);
    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public async Task CreateAsync_NullRoleId_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input() with { RoleId = null }));

    Assert.Contains(nameof(HypothesisInput.RoleId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownRoleId_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(roleId: Guid.NewGuid())));

    Assert.Contains(nameof(HypothesisInput.RoleId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NullLanguageId_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input() with { LanguageId = null }));

    Assert.Contains(nameof(HypothesisInput.LanguageId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownLanguageId_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(languageId: Guid.NewGuid())));

    Assert.Contains(nameof(HypothesisInput.LanguageId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownLocationId_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(locationIds: [SeedData.EuLocationId, Guid.NewGuid()])));

    Assert.Contains(nameof(HypothesisInput.LocationIds), exception.Errors.Keys);
    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public async Task CreateAsync_WorkFormatNone_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(workFormat: WorkFormat.None)));

    Assert.Contains(nameof(HypothesisInput.WorkFormat), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_SeveralInvalidFields_CollectsAllErrors()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(name: "", workFormat: WorkFormat.None) with { RoleId = null, LanguageId = null }));

    Assert.Equal(4, exception.Errors.Count);
  }

  [Fact]
  public async Task CreateAsync_DuplicateLocationIds_StoresEachOnce()
  {
    var created = await Service.CreateAsync(Input(locationIds: [SeedData.EuLocationId, SeedData.EuLocationId, SeedData.UsaLocationId]));

    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.UsaLocationId }.Order(), created.LocationIds.Order());
  }

  [Fact]
  public async Task CreateAsync_Industries_TrimsDropsBlanksAndDedupesCaseInsensitively()
  {
    var created = await Service.CreateAsync(Input(industries: ["Games", "games ", " ", "Tools"]));

    Assert.Equal(["Games", "Tools"], created.Industries);
  }

  [Theory]
  [InlineData(null, null)]
  [InlineData("", null)]
  [InlineData("   ", null)]
  [InlineData("  Tools expert ", "Tools expert")]
  public async Task CreateAsync_Positioning_BlankBecomesNullElseTrimmed(string? positioning, string? expected)
  {
    var created = await Service.CreateAsync(Input(positioning: positioning));

    Assert.Equal(expected, created.Positioning);
  }

  [Fact]
  public async Task CreateAsync_UnknownPrimaryCv_ThrowsValidationWithKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(primaryCvDocumentId: Guid.NewGuid())));

    Assert.Contains(nameof(HypothesisInput.PrimaryCvDocumentId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_PrimaryCvOfAnotherUser_ThrowsValidationWithKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var (cv, _) = await PersistenceFixtures.AddCvAsync(Database, other.Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(primaryCvDocumentId: cv.Id)));

    Assert.Contains(nameof(HypothesisInput.PrimaryCvDocumentId), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_DraftStatus_LeavesStartedAtNull()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Draft));

    Assert.Equal(HypothesisStatus.Draft, created.Status);
    Assert.Null(created.StartedAt);
  }

  [Fact]
  public async Task CreateAsync_ActiveStatus_SetsStartedAtToNow()
  {
    var created = await Service.CreateAsync(Input(status: HypothesisStatus.Active));

    Assert.Equal(HypothesisStatus.Active, created.Status);
    Assert.Equal(Now, created.StartedAt);
  }

  [Fact]
  public async Task UpdateAsync_ChangesAllFields()
  {
    var created = await Service.CreateAsync(Input());

    await Service.UpdateAsync(created.Id, Input(
      name: "Unreal EN",
      roleId: SeedData.UnrealDeveloperRoleId,
      locationIds: [SeedData.SerbiaLocationId],
      industries: ["Film"],
      workFormat: WorkFormat.Hybrid,
      languageId: SeedData.EnglishLanguageId,
      positioning: "Graphics"));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal("Unreal EN", found.Name);
    Assert.Equal(SeedData.UnrealDeveloperRoleId, found.RoleId);
    Assert.Equal([SeedData.SerbiaLocationId], found.LocationIds);
    Assert.Equal(["Film"], found.Industries);
    Assert.Equal(WorkFormat.Hybrid, found.WorkFormat);
    Assert.Equal(SeedData.EnglishLanguageId, found.LanguageId);
    Assert.Equal("Graphics", found.Positioning);
  }

  [Fact]
  public async Task UpdateAsync_ReplacingLocations_RemovesOldOnes()
  {
    var created = await Service.CreateAsync(Input(locationIds: [SeedData.EuLocationId, SeedData.UsaLocationId]));

    await Service.UpdateAsync(created.Id, Input(locationIds: [SeedData.UsaLocationId, SeedData.SerbiaLocationId]));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(new[] { SeedData.UsaLocationId, SeedData.SerbiaLocationId }.Order(), found!.LocationIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, await context.HypothesisLocations.CountAsync(l => l.HypothesisId == created.Id));
  }

  [Fact]
  public async Task UpdateAsync_EmptyLocations_ClearsThem()
  {
    var created = await Service.CreateAsync(Input(locationIds: [SeedData.EuLocationId]));

    await Service.UpdateAsync(created.Id, Input(locationIds: []));

    Assert.Empty((await Service.FindAsync(created.Id))!.LocationIds);
  }

  [Fact]
  public async Task UpdateAsync_DraftToActive_SetsStartedAtToUpdateTime()
  {
    var created = await Service.CreateAsync(Input());
    var later = Now.AddDays(2);
    Clock.UtcNow = later;

    var updated = await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Active));

    Assert.Equal(later, updated.StartedAt);
  }

  [Fact]
  public async Task UpdateAsync_ActiveAgain_KeepsStartedAt()
  {
    var created = await Service.CreateAsync(Input());
    Clock.UtcNow = Now.AddDays(2);
    await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Active));
    Clock.UtcNow = Now.AddDays(5);

    var updated = await Service.UpdateAsync(created.Id, Input(status: HypothesisStatus.Expanded));

    Assert.Equal(Now.AddDays(2), updated.StartedAt);
  }

  [Fact]
  public async Task UpdateAsync_InvalidInput_ThrowsValidationAndKeepsStoredValues()
  {
    var created = await Service.CreateAsync(Input());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input() with { RoleId = null }));

    Assert.Contains(nameof(HypothesisInput.RoleId), exception.Errors.Keys);
    Assert.Equal(SeedData.UnityDeveloperRoleId, (await Service.FindAsync(created.Id))!.RoleId);
  }

  [Fact]
  public async Task UpdateAsync_MissingHypothesis_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(Guid.NewGuid(), Input()));
  }

  [Fact]
  public async Task UpdateAsync_HypothesisOfAnotherUser_ThrowsKeyNotFound()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(foreign.Id, Input()));
  }

  [Fact]
  public async Task FindAsync_Missing_ReturnsNull()
  {
    Assert.Null(await Service.FindAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task FindAsync_HypothesisOfAnotherUser_ReturnsNull()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input());

    Assert.Null(await Service.FindAsync(foreign.Id));
  }
}
