using Karajob.Application.Common;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisServiceCrudTests : HypothesisServiceFixture
{
  [Fact]
  public async Task CreateAsync_ValidInput_ReturnsAndPersistsDetails()
  {
    var (cv, _) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);

    var created = await Service.CreateAsync(Input(
      locations: ["EU", "US"],
      industries: ["Games"],
      workFormat: WorkFormat.Remote | WorkFormat.Relocation,
      positioning: "Tools expert",
      primaryCvDocumentId: cv.Id));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal("Unity Developer RU", found.Name);
    Assert.Equal("Unity Developer", found.Role);
    Assert.Equal(["EU", "US"], found.Locations);
    Assert.Equal(["Games"], found.Industries);
    Assert.Equal(WorkFormat.Remote | WorkFormat.Relocation, found.WorkFormat);
    Assert.Equal("ru", found.Language);
    Assert.Equal("Tools expert", found.Positioning);
    Assert.Equal(cv.Id, found.PrimaryCvDocumentId);
  }

  [Fact]
  public async Task CreateAsync_TextWithWhitespaceAndUpperCaseLanguage_TrimsAndLowerCases()
  {
    var created = await Service.CreateAsync(Input(name: "  Name  ", role: "  Role ", language: " RU "));

    Assert.Equal("Name", created.Name);
    Assert.Equal("Role", created.Role);
    Assert.Equal("ru", created.Language);
  }

  [Theory]
  [InlineData("", "r", "ru", nameof(HypothesisInput.Name))]
  [InlineData("   ", "r", "ru", nameof(HypothesisInput.Name))]
  [InlineData("n", "", "ru", nameof(HypothesisInput.Role))]
  [InlineData("n", "  ", "ru", nameof(HypothesisInput.Role))]
  [InlineData("n", "r", "", nameof(HypothesisInput.Language))]
  [InlineData("n", "r", " ", nameof(HypothesisInput.Language))]
  public async Task CreateAsync_BlankRequiredField_ThrowsValidationWithKey(string name, string role, string language, string key)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(name: name, role: role, language: language)));

    Assert.Contains(key, exception.Errors.Keys);
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
      () => Service.CreateAsync(Input(name: "", role: "", language: "", workFormat: WorkFormat.None)));

    Assert.Equal(4, exception.Errors.Count);
  }

  [Fact]
  public async Task CreateAsync_LocationsAndIndustries_TrimsDropsBlanksAndDedupesCaseInsensitively()
  {
    var created = await Service.CreateAsync(Input(
      locations: [" EU ", "", "  ", "eu", "US", "Eu"],
      industries: ["Games", "games ", " ", "Tools"]));

    Assert.Equal(["EU", "US"], created.Locations);
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
      role: "Unreal Developer",
      locations: ["UK"],
      industries: ["Film"],
      workFormat: WorkFormat.Hybrid,
      language: "EN",
      positioning: "Graphics"));

    var found = await Service.FindAsync(created.Id);
    Assert.NotNull(found);
    Assert.Equal("Unreal EN", found.Name);
    Assert.Equal("Unreal Developer", found.Role);
    Assert.Equal(["UK"], found.Locations);
    Assert.Equal(["Film"], found.Industries);
    Assert.Equal(WorkFormat.Hybrid, found.WorkFormat);
    Assert.Equal("en", found.Language);
    Assert.Equal("Graphics", found.Positioning);
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

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, Input(role: " ")));

    Assert.Contains(nameof(HypothesisInput.Role), exception.Errors.Keys);
    Assert.Equal("Unity Developer", (await Service.FindAsync(created.Id))!.Role);
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
