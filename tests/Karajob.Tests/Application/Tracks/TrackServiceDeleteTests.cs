using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Tracks;

public class TrackServiceDeleteTests : TrackServiceFixture
{
  [Fact]
  public async Task DeleteAsync_Unreferenced_RemovesTrack()
  {
    var created = await Service.CreateAsync(Input());

    await Service.DeleteAsync(created.Id);

    Assert.Null(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_Missing_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_TrackOfAnotherUser_ThrowsKeyNotFoundAndKeepsIt()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherService = ServiceFor(other.Id);
    var foreign = await otherService.CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(foreign.Id));

    Assert.NotNull(await otherService.FindAsync(foreign.Id));
  }

  [Fact]
  public async Task DeleteAsync_ReferencedByVacancyMatch_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    var created = await Service.CreateAsync(Input());
    await PersistenceFixtures.AddMatchedVacancyAsync(Database, SeedData.OwnerId, created.Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_ReferencedByApplication_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    var (_, track, _) = await PersistenceFixtures.AddAppliedVacancyAsync(Database, SeedData.OwnerId);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(track.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(track.Id));
  }
}
