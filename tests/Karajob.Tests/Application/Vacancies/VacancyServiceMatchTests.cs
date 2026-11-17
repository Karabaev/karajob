using Karajob.Application.Common;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceMatchTests : VacancyServiceFixture
{
  [Fact]
  public async Task CreateAsync_TwoTracks_DetailsContainBoth()
  {
    var first = await CreateTrackAsync("First");
    var second = await CreateTrackAsync("Second");

    var details = await Service.CreateAsync(Input(trackIds: [first, second]));

    Assert.Equal(new[] { first, second }.Order(), details.TrackIds.Order());
    var found = await Service.FindAsync(details.Id);
    Assert.Equal(new[] { first, second }.Order(), found!.TrackIds.Order());
  }

  [Fact]
  public async Task CreateAsync_DisabledTrack_FailsWithTrackError()
  {
    var disabled = await CreateTrackAsync("Off", status: TrackStatus.Disabled);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(trackIds: [disabled])));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task UpdateAsync_NewlyAddedDisabledTrack_Fails()
  {
    var created = await Service.CreateAsync(Input());
    var disabled = await CreateTrackAsync("Off", status: TrackStatus.Disabled);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, Input(trackIds: [disabled])));

    Assert.Contains("TrackIds", ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_AlreadyMatchedTrackThenDisabled_StaysAllowed()
  {
    var trackId = await CreateTrackAsync("Soon off");
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateTrackAsync(trackId, h => h.Status = TrackStatus.Disabled);

    var updated = await Service.UpdateAsync(created.Id, Input(trackIds: [trackId], notes: "kept"));

    Assert.Equal([trackId], updated.TrackIds);
  }

  [Fact]
  public async Task CreateAsync_DuplicateTrackIds_AreTolerated()
  {
    var first = await CreateTrackAsync("First");

    var details = await Service.CreateAsync(Input(trackIds: [first, first]));

    Assert.Equal([first], details.TrackIds);
  }

  [Fact]
  public async Task UpdateAsync_AddsNewAndRemovesUncheckedTracks()
  {
    var first = await CreateTrackAsync("First");
    var second = await CreateTrackAsync("Second");
    var third = await CreateTrackAsync("Third");
    var created = await Service.CreateAsync(Input(trackIds: [first, second]));

    var updated = await Service.UpdateAsync(created.Id, Input(trackIds: [second, third]));

    Assert.Equal(new[] { second, third }.Order(), updated.TrackIds.Order());
    var stored = await LoadAsync(created.Id);
    Assert.Equal(new[] { second, third }.Order(), stored.Matches.Select(m => m.TrackId).Order());
  }

  [Fact]
  public async Task UpdateAsync_KeptMatch_PreservesScoreRationaleAndAuthor()
  {
    var first = await CreateTrackAsync("First");
    var second = await CreateTrackAsync("Second");
    var created = await Service.CreateAsync(Input(trackIds: [first]));
    await MutateAsync(created.Id, v => v.SetMatch(first, 80, "fit", MatchAuthor.Ai));

    await Service.UpdateAsync(created.Id, Input(trackIds: [first, second]));

    var stored = await LoadAsync(created.Id);
    var kept = stored.Matches.Single(m => m.TrackId == first);
    Assert.Equal(80, kept.Score);
    Assert.Equal("fit", kept.Rationale);
    Assert.Equal(MatchAuthor.Ai, kept.MatchedBy);
    Assert.Equal(2, stored.Matches.Count);
  }

  [Fact]
  public async Task UpdateAsync_UnknownTrack_FailsWithTrackIdsError()
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(trackIds: [Guid.NewGuid()])));

    Assert.Contains("TrackIds", ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_TargetedVacancyRemovingAllTracks_FailsAndChangesNothing()
  {
    var first = await CreateTrackAsync("First");
    var created = await Service.CreateAsync(Input(trackIds: [first], notes: "keep"));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(trackIds: [], notes: "lost")));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal([first], stored.Matches.Select(m => m.TrackId));
    Assert.Equal("keep", stored.Notes);
  }

  [Fact]
  public async Task UpdateAsync_InboxVacancyRemovingAllTracks_Succeeds()
  {
    var first = await CreateTrackAsync("First");
    var created = await Service.CreateAsync(Input(trackIds: [first]));

    var updated = await Service.UpdateAsync(created.Id, Input(trackIds: []));

    Assert.Empty(updated.TrackIds);
  }

  [Fact]
  public async Task UpdateAsync_AppliedVacancyRemovingApplicationTrack_FailsWithTrackIdsError()
  {
    var (vacancyId, applied, other) = await CreateAppliedVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(vacancyId, Input(trackIds: [other])));

    Assert.Contains("TrackIds", ex.Errors.Keys);
    var stored = await LoadAsync(vacancyId);
    Assert.Contains(stored.Matches, m => m.TrackId == applied);
  }

  [Fact]
  public async Task UpdateAsync_AppliedVacancyRemovingOtherTrack_Succeeds()
  {
    var (vacancyId, applied, _) = await CreateAppliedVacancyAsync();

    var updated = await Service.UpdateAsync(vacancyId, Input(trackIds: [applied]));

    Assert.Equal([applied], updated.TrackIds);
  }

  [Fact]
  public async Task FindAsync_AppliedVacancy_ExposesApplicationTrackId()
  {
    var (vacancyId, applied, _) = await CreateAppliedVacancyAsync();

    var found = await Service.FindAsync(vacancyId);

    Assert.Equal(applied, found!.ApplicationTrackId);
    Assert.Equal(VacancyStage.Applied, found.Stage);
  }

  private async Task<(Guid VacancyId, Guid Applied, Guid Other)> CreateAppliedVacancyAsync()
  {
    var applied = await CreateTrackAsync("Applied");
    var other = await CreateTrackAsync("Other");
    var created = await Service.CreateAsync(Input(trackIds: [applied, other]));
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);
    await MutateAsync(created.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(applied, cvVersion.Id, ApplicationChannel.Website, Now.AddHours(1), null);
    });
    return (created.Id, applied, other);
  }
}
