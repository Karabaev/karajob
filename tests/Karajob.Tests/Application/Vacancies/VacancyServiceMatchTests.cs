using Karajob.Application.Common;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceMatchTests : VacancyServiceFixture
{
  [Fact]
  public async Task CreateAsync_TwoHypotheses_DetailsContainBoth()
  {
    var first = await CreateHypothesisAsync("First");
    var second = await CreateHypothesisAsync("Second");

    var details = await Service.CreateAsync(Input(hypothesisIds: [first, second]));

    Assert.Equal(new[] { first, second }.Order(), details.HypothesisIds.Order());
    var found = await Service.FindAsync(details.Id);
    Assert.Equal(new[] { first, second }.Order(), found!.HypothesisIds.Order());
  }

  [Fact]
  public async Task CreateAsync_DuplicateHypothesisIds_AreTolerated()
  {
    var first = await CreateHypothesisAsync("First");

    var details = await Service.CreateAsync(Input(hypothesisIds: [first, first]));

    Assert.Equal([first], details.HypothesisIds);
  }

  [Fact]
  public async Task UpdateAsync_AddsNewAndRemovesUncheckedHypotheses()
  {
    var first = await CreateHypothesisAsync("First");
    var second = await CreateHypothesisAsync("Second");
    var third = await CreateHypothesisAsync("Third");
    var created = await Service.CreateAsync(Input(hypothesisIds: [first, second]));

    var updated = await Service.UpdateAsync(created.Id, Input(hypothesisIds: [second, third]));

    Assert.Equal(new[] { second, third }.Order(), updated.HypothesisIds.Order());
    var stored = await LoadAsync(created.Id);
    Assert.Equal(new[] { second, third }.Order(), stored.Matches.Select(m => m.HypothesisId).Order());
  }

  [Fact]
  public async Task UpdateAsync_KeptMatch_PreservesScoreRationaleAndAuthor()
  {
    var first = await CreateHypothesisAsync("First");
    var second = await CreateHypothesisAsync("Second");
    var created = await Service.CreateAsync(Input(hypothesisIds: [first]));
    await MutateAsync(created.Id, v => v.SetMatch(first, 80, "fit", MatchAuthor.Ai));

    await Service.UpdateAsync(created.Id, Input(hypothesisIds: [first, second]));

    var stored = await LoadAsync(created.Id);
    var kept = stored.Matches.Single(m => m.HypothesisId == first);
    Assert.Equal(80, kept.Score);
    Assert.Equal("fit", kept.Rationale);
    Assert.Equal(MatchAuthor.Ai, kept.MatchedBy);
    Assert.Equal(2, stored.Matches.Count);
  }

  [Fact]
  public async Task UpdateAsync_UnknownHypothesis_FailsWithHypothesisIdsError()
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(hypothesisIds: [Guid.NewGuid()])));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_TargetedVacancyRemovingAllHypotheses_FailsAndChangesNothing()
  {
    var first = await CreateHypothesisAsync("First");
    var created = await Service.CreateAsync(Input(hypothesisIds: [first], notes: "keep"));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(hypothesisIds: [], notes: "lost")));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    var stored = await LoadAsync(created.Id);
    Assert.Equal([first], stored.Matches.Select(m => m.HypothesisId));
    Assert.Equal("keep", stored.Notes);
  }

  [Fact]
  public async Task UpdateAsync_InboxVacancyRemovingAllHypotheses_Succeeds()
  {
    var first = await CreateHypothesisAsync("First");
    var created = await Service.CreateAsync(Input(hypothesisIds: [first]));

    var updated = await Service.UpdateAsync(created.Id, Input(hypothesisIds: []));

    Assert.Empty(updated.HypothesisIds);
  }

  [Fact]
  public async Task UpdateAsync_AppliedVacancyRemovingApplicationHypothesis_FailsWithHypothesisIdsError()
  {
    var (vacancyId, applied, other) = await CreateAppliedVacancyAsync();

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(vacancyId, Input(hypothesisIds: [other])));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    var stored = await LoadAsync(vacancyId);
    Assert.Contains(stored.Matches, m => m.HypothesisId == applied);
  }

  [Fact]
  public async Task UpdateAsync_AppliedVacancyRemovingOtherHypothesis_Succeeds()
  {
    var (vacancyId, applied, _) = await CreateAppliedVacancyAsync();

    var updated = await Service.UpdateAsync(vacancyId, Input(hypothesisIds: [applied]));

    Assert.Equal([applied], updated.HypothesisIds);
  }

  [Fact]
  public async Task FindAsync_AppliedVacancy_ExposesApplicationHypothesisId()
  {
    var (vacancyId, applied, _) = await CreateAppliedVacancyAsync();

    var found = await Service.FindAsync(vacancyId);

    Assert.Equal(applied, found!.ApplicationHypothesisId);
    Assert.Equal(VacancyStage.Applied, found.Stage);
  }

  private async Task<(Guid VacancyId, Guid Applied, Guid Other)> CreateAppliedVacancyAsync()
  {
    var applied = await CreateHypothesisAsync("Applied");
    var other = await CreateHypothesisAsync("Other");
    var created = await Service.CreateAsync(Input(hypothesisIds: [applied, other]));
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);
    await MutateAsync(created.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(applied, cvVersion.Id, ApplicationChannel.Website, Now.AddHours(1), null);
    });
    return (created.Id, applied, other);
  }
}
