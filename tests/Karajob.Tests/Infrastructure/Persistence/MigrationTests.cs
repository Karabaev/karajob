using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class MigrationTests
{
  [Fact]
  public async Task Model_HasNoPendingChangesComparedToMigrations()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    Assert.False(context.Database.HasPendingModelChanges());
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_AppliesInitialMigration()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var applied = await context.Database.GetAppliedMigrationsAsync();

    Assert.Contains(applied, m => m.EndsWith("_Initial", StringComparison.Ordinal));
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_LeavesNoPendingMigrations()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var pending = await context.Database.GetPendingMigrationsAsync();

    Assert.Empty(pending);
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_SeedsOwnerUser()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var owner = await context.Users.SingleAsync(u => u.Id == SeedData.OwnerId);

    Assert.Equal(SeedData.OwnerName, owner.Name);
    Assert.Equal(SeedData.OwnerCreatedAt, owner.CreatedAt);
    Assert.Equal(DateTimeKind.Utc, owner.CreatedAt.Kind);
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_SeedsRoles()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var roles = await context.Roles.OrderBy(r => r.Name).ToListAsync();

    Assert.Equal(
      [
        (SeedData.UnityDeveloperRoleId, "Unity Developer", "unity developer"),
        (SeedData.UnrealDeveloperRoleId, "Unreal Developer", "unreal developer"),
      ],
      roles.Select(r => (r.Id, r.Name, r.NormalizedName)));
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_SeedsLocations()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var locations = await context.Locations.OrderBy(l => l.Name).ToListAsync();

    Assert.Equal(
      [
        (SeedData.EuLocationId, "EU", "eu"),
        (SeedData.SerbiaLocationId, "Serbia", "serbia"),
        (SeedData.UsaLocationId, "USA", "usa"),
      ],
      locations.Select(l => (l.Id, l.Name, l.NormalizedName)));
  }

  [Fact]
  public async Task Migrate_EmptyDatabase_SeedsLanguages()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var languages = await context.Languages.OrderBy(l => l.Code).ToListAsync();

    Assert.Equal(
      [
        (SeedData.EnglishLanguageId, "en", "English", "english"),
        (SeedData.RussianLanguageId, "ru", "Русский", "русский"),
      ],
      languages.Select(l => (l.Id, l.Code, l.Name, l.NormalizedName)));
  }
}
