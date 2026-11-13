using Karajob.Domain.Common;
using Karajob.Domain.Companies;
using Karajob.Domain.Users;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class UserFilterTests
{
  [Fact]
  public async Task Vacancies_SavedForOtherUser_AreHiddenFromCurrentUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddVacancyAsync(database, other.Id);

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.Vacancies.ToListAsync());
    Assert.Single(await otherContext.Vacancies.ToListAsync());
  }

  [Fact]
  public async Task Hypotheses_SavedForOtherUser_AreHiddenFromCurrentUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddHypothesisAsync(database, other.Id);

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.Hypotheses.ToListAsync());
    Assert.Single(await otherContext.Hypotheses.ToListAsync());
  }

  [Fact]
  public async Task CvDocuments_SavedForOtherUser_AreHiddenFromCurrentUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    await PersistenceFixtures.AddCvAsync(database, other.Id);

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.CvDocuments.ToListAsync());
    Assert.Single(await otherContext.CvDocuments.ToListAsync());
  }

  [Fact]
  public async Task Companies_Saved_AreVisibleToAllUsers()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    var company = await PersistenceFixtures.AddCompanyAsync(database, "shared-co");

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.True(await owner.Companies.AnyAsync(c => c.Id == company.Id));
    Assert.True(await otherContext.Companies.AnyAsync(c => c.Id == company.Id));
  }

  [Fact]
  public async Task Model_UserOwnedEntityTypes_HaveUserIdQueryFilter()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var owned = context.Model.GetEntityTypes().Where(t => typeof(IUserOwned).IsAssignableFrom(t.ClrType)).ToList();

    Assert.NotEmpty(owned);
    Assert.All(owned, t => Assert.NotNull(t.FindDeclaredQueryFilter("UserId")));
  }

  [Theory]
  [InlineData(typeof(User))]
  [InlineData(typeof(Company))]
  [InlineData(typeof(CompanyDomain))]
  [InlineData(typeof(CompanyAlias))]
  [InlineData(typeof(Project))]
  public async Task Model_SharedEntityTypes_HaveNoQueryFilter(Type clrType)
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var entityType = context.Model.FindEntityType(clrType);

    Assert.NotNull(entityType);
    Assert.Empty(entityType.GetDeclaredQueryFilters());
  }
}
