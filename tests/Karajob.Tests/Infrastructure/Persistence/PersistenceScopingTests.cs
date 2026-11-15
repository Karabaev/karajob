using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Users;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Infrastructure.Persistence;

public class PersistenceScopingTests
{
  private static readonly DatabaseOptions InMemory = new() { Provider = DatabaseOptions.SqliteInMemoryProvider };

  [Fact]
  public void AddKarajobPersistence_RegistersEfContextFactoryAsScoped()
  {
    var services = new ServiceCollection();

    services.AddKarajobPersistence(InMemory);

    var descriptor = services.Last(d => d.ServiceType == typeof(IDbContextFactory<KarajobDbContext>));
    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
  }

  [Fact]
  public void AddKarajobPersistence_RegistersApplicationContextFactoryAsScoped()
  {
    var services = new ServiceCollection();

    services.AddKarajobPersistence(InMemory);

    var descriptor = services.Last(d => d.ServiceType == typeof(IKarajobDbContextFactory));
    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
  }

  [Fact]
  public async Task CreateDbContextAsync_TwoScopesWithDifferentUsers_EachSeesOnlyOwnVacancies()
  {
    await using var provider = BuildProvider();
    var other = await PrepareDatabaseWithOtherUserAsync(provider);
    await AddVacancyInScopeAsync(provider, SeedData.OwnerId, "https://example.com/owner");
    await AddVacancyInScopeAsync(provider, other.Id, "https://example.com/other");

    var ownerUrls = await ListVacancyUrlsInScopeAsync(provider, SeedData.OwnerId);
    var otherUrls = await ListVacancyUrlsInScopeAsync(provider, other.Id);

    // The in-memory initializer also seeds development vacancies for Owner.
    Assert.Contains("https://example.com/owner", ownerUrls);
    Assert.DoesNotContain("https://example.com/other", ownerUrls);
    Assert.Equal(["https://example.com/other"], otherUrls);
  }

  [Fact]
  public async Task CreateDbContextAsync_UserNotSelectedYet_StillCreatesMigratesAndSavesUser()
  {
    await using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IKarajobDbContextFactory>();

    await using var context = Assert.IsType<KarajobDbContext>(await factory.CreateDbContextAsync());
    await context.Database.MigrateAsync();
    context.Users.Add(new User("Anna", "#26a69a", DateTime.UtcNow));
    await context.SaveChangesAsync();

    Assert.Equal(2, await context.Users.CountAsync());
  }

  [Fact]
  public async Task Query_UserNotSelectedYet_ThrowsInvalidOperationException()
  {
    await using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IKarajobDbContextFactory>();
    await using var context = Assert.IsType<KarajobDbContext>(await factory.CreateDbContextAsync());
    await context.Database.MigrateAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => context.Vacancies.ToListAsync());
  }

  [Fact]
  public async Task InitializeKarajobDatabaseAsync_ScopedCurrentUserWithoutUser_MigratesAndSeedsWithoutRootResolution()
  {
    await using var provider = BuildProvider();

    await provider.InitializeKarajobDatabaseAsync();

    Assert.Equal(3, await CountHypothesesInScopeAsync(provider, SeedData.OwnerId));
  }

  [Fact]
  public async Task AddKarajobPersistence_SingletonCurrentUser_ResolvesScopedContextFactory()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<ICurrentUser>(new Karajob.Infrastructure.Users.FixedCurrentUser(SeedData.OwnerId));
    services.AddKarajobPersistence(InMemory);
    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
    await provider.InitializeKarajobDatabaseAsync();
    using var scope = provider.CreateScope();

    var factory = scope.ServiceProvider.GetRequiredService<IKarajobDbContextFactory>();
    await using var context = await factory.CreateDbContextAsync();

    Assert.Equal(3, await context.Hypotheses.CountAsync());
  }

  private static ServiceProvider BuildProvider()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddScoped<ScopedCurrentUser>();
    services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ScopedCurrentUser>());
    services.AddKarajobPersistence(InMemory);
    return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
  }

  private static async Task<User> PrepareDatabaseWithOtherUserAsync(ServiceProvider provider)
  {
    await provider.InitializeKarajobDatabaseAsync();
    var other = new User("Second", "#26a69a", DateTime.UtcNow);
    using var scope = provider.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    context.Users.Add(other);
    await context.SaveChangesAsync();
    return other;
  }

  private static async Task AddVacancyInScopeAsync(ServiceProvider provider, Guid userId, string url)
  {
    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<ScopedCurrentUser>().Id = userId;
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    context.Vacancies.Add(new Karajob.Domain.Vacancies.Vacancy(userId, url, Karajob.Domain.Vacancies.VacancySource.Manual, DateTime.UtcNow));
    await context.SaveChangesAsync();
  }

  private static async Task<List<string>> ListVacancyUrlsInScopeAsync(ServiceProvider provider, Guid userId)
  {
    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<ScopedCurrentUser>().Id = userId;
    var factory = scope.ServiceProvider.GetRequiredService<IKarajobDbContextFactory>();
    await using var context = await factory.CreateDbContextAsync();
    return await context.Vacancies.Select(v => v.Url).ToListAsync();
  }

  private static async Task<int> CountHypothesesInScopeAsync(ServiceProvider provider, Guid userId)
  {
    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<ScopedCurrentUser>().Id = userId;
    var factory = scope.ServiceProvider.GetRequiredService<IKarajobDbContextFactory>();
    await using var context = await factory.CreateDbContextAsync();
    return await context.Hypotheses.CountAsync();
  }

  private class ScopedCurrentUser : ICurrentUser
  {
    public Guid? Id { get; set; }

    public Guid UserId => Id ?? throw new InvalidOperationException("No profile selected.");
  }
}
