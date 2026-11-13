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
}
