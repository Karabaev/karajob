using Karajob.Domain.Catalogs;
using Karajob.Domain.Companies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class UniqueIndexTests
{
  [Fact]
  public async Task SaveChanges_DuplicateCompanySlug_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddCompanyAsync(database, "same-slug");
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Companies.Add(new Company("same-slug", "Other"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameDomainOnTwoCompanies_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var first = new Company("dom-one", "One");
    first.AddDomain("shared.example.com");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(first);
      await context.SaveChangesAsync();
    }

    await using var second = database.CreateContext(SeedData.OwnerId);
    var other = new Company("dom-two", "Two");
    other.AddDomain("shared.example.com");
    second.Companies.Add(other);

    await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameAliasOnTwoCompanies_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var first = new Company("alias-one", "One");
    first.AddAlias("Shared Alias");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(first);
      await context.SaveChangesAsync();
    }

    await using var second = database.CreateContext(SeedData.OwnerId);
    var other = new Company("alias-two", "Two");
    other.AddAlias("Shared Alias");
    second.Companies.Add(other);

    await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameNormalizedVacancyUrlForSameUser_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddVacancyAsync(database, SeedData.OwnerId, "https://example.com/jobs/42");
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Vacancies.Add(new Vacancy(
      SeedData.OwnerId,
      "https://www.example.com/jobs/42/?utm_source=newsletter",
      VacancySource.Manual,
      PersistenceFixtures.Moment));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameVacancyUrlForDifferentUsers_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddVacancyAsync(database, SeedData.OwnerId, "https://example.com/jobs/42");

    await PersistenceFixtures.AddVacancyAsync(database, other.Id, "https://example.com/jobs/42");

    await using var context = database.CreateContext(other.Id);
    Assert.Equal(1, await context.Vacancies.CountAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateUserCompanyNote_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = await PersistenceFixtures.AddCompanyAsync(database, "noted-co");
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.UserCompanyNotes.Add(new UserCompanyNote(SeedData.OwnerId, company.Id));
    context.UserCompanyNotes.Add(new UserCompanyNote(SeedData.OwnerId, company.Id));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateVacancyMatchRow_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);
    var twin = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/twin", VacancySource.Manual, PersistenceFixtures.Moment);
    var duplicate = twin.SetMatch(hypothesis.Id, 10, null, MatchAuthor.Human);
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Vacancies.Add(twin);
    context.Entry(duplicate).Property(m => m.VacancyId).CurrentValue = vacancy.Id;

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateRoleNormalizedName_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Roles.Add(new Role("UNITY  developer"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateLocationNormalizedName_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Locations.Add(new Location(" eu "));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateLanguageNormalizedName_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Languages.Add(new Language("xx", "ENGLISH"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DuplicateLanguageCode_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Languages.Add(new Language("RU", "Another name"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_SameNameAcrossCatalogKinds_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Roles.Add(new Role("Serbia"));
    context.Locations.Add(new Location("Unity Developer"));

    await context.SaveChangesAsync();

    Assert.Equal(1, await context.Roles.CountAsync(r => r.NormalizedName == "serbia"));
  }
}
