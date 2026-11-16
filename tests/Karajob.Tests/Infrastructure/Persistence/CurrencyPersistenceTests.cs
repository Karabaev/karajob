using Karajob.Domain.Catalogs;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class CurrencyPersistenceTests
{
  [Fact]
  public async Task Currencies_AfterMigration_ContainSeededCatalog()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var currencies = await context.Currencies.ToListAsync();

    Assert.Equal(["EUR", "RUB", "USD"], currencies.Select(c => c.Code).Order(StringComparer.Ordinal));
    Assert.Equal(SeedData.RubCurrencyId, currencies.Single(c => c.Code == "RUB").Id);
    Assert.Equal("Russian ruble", currencies.Single(c => c.Code == "RUB").Name);
    Assert.Equal("US dollar", currencies.Single(c => c.Code == "USD").Name);
    Assert.Equal("Euro", currencies.Single(c => c.Code == "EUR").Name);
  }

  [Fact]
  public async Task SaveChanges_SearchGoalWithKnownIncomeCurrency_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.SetDesiredIncome(5000, "USD");

    await context.SaveChangesAsync();

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Equal("USD", (await fresh.SearchGoals.SingleAsync()).IncomeCurrency);
  }

  [Fact]
  public async Task SaveChanges_SearchGoalWithIncomeCurrencyNotInCatalog_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.SetDesiredIncome(5000, "GBP");

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingCurrencyReferencedBySearchGoal_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var goal = await context.SearchGoals.SingleAsync();
      goal.SetDesiredIncome(5000, "EUR");
      await context.SaveChangesAsync();
    }

    await using var deleting = database.CreateContext(SeedData.OwnerId);
    deleting.Currencies.Remove(await deleting.Currencies.SingleAsync(c => c.Code == "EUR"));

    await Assert.ThrowsAsync<DbUpdateException>(() => deleting.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingUnreferencedCurrency_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Currencies.Remove(await context.Currencies.SingleAsync(c => c.Code == "EUR"));

    await context.SaveChangesAsync();

    Assert.False(await context.Currencies.AnyAsync(c => c.Code == "EUR"));
  }

  [Fact]
  public async Task SaveChanges_CurrencyWithDuplicateCode_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Currencies.Add(new Currency("RUB", "Другой рубль"));

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }
}
