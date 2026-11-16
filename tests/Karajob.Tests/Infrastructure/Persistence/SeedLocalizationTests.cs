using Karajob.Domain.Catalogs;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class SeedLocalizationTests
{
  [Fact]
  public async Task Languages_AfterMigration_RussianIsNamedInEnglish()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var russian = await context.Languages.SingleAsync(l => l.Code == "ru");

    Assert.Equal("Russian", russian.Name);
    Assert.Equal("russian", russian.NormalizedName);
  }

  [Theory]
  [InlineData("RUB", "Russian ruble")]
  [InlineData("USD", "US dollar")]
  [InlineData("EUR", "Euro")]
  public async Task Currencies_AfterMigration_AreNamedInEnglish(string code, string name)
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var currency = await context.Currencies.SingleAsync(c => c.Code == code);

    Assert.Equal(name, currency.Name);
  }

  [Fact]
  public async Task Engines_AfterMigration_ProprietaryEngineIsNamedInEnglish()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var engine = await context.Engines.SingleAsync(e => e.Id == SeedData.ProprietaryEngineId);

    Assert.Equal("Proprietary", engine.Name);
    Assert.Equal("proprietary", engine.NormalizedName);
  }

  [Fact]
  public async Task SeededCatalogs_AfterMigration_HaveNoCyrillicNames()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var names = (await Names(context)).SelectMany(n => n).ToList();

    Assert.NotEmpty(names);
    Assert.DoesNotContain(names, n => n.Any(c => c is >= 'Ѐ' and <= 'ӿ'));
  }

  [Fact]
  public async Task SeededCatalogs_AfterMigration_NormalizedNameIsLowercasedName()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var entries = new List<CatalogEntry>();
    entries.AddRange(await context.Roles.ToListAsync());
    entries.AddRange(await context.Locations.ToListAsync());
    entries.AddRange(await context.Languages.ToListAsync());
    entries.AddRange(await context.Platforms.ToListAsync());
    entries.AddRange(await context.Currencies.ToListAsync());
    entries.AddRange(await context.Engines.ToListAsync());

    Assert.NotEmpty(entries);
    Assert.All(entries, e => Assert.Equal(e.Name.ToLowerInvariant(), e.NormalizedName));
  }

  private static async Task<List<List<string>>> Names(KarajobDbContext context)
  {
    return
    [
      await context.Roles.Select(e => e.Name).ToListAsync(),
      await context.Locations.Select(e => e.Name).ToListAsync(),
      await context.Languages.Select(e => e.Name).ToListAsync(),
      await context.Platforms.Select(e => e.Name).ToListAsync(),
      await context.Currencies.Select(e => e.Name).ToListAsync(),
      await context.Engines.Select(e => e.Name).ToListAsync(),
    ];
  }
}
