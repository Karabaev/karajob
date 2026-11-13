using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class PersistenceCompositionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Fact]
  public void Host_ResolvesDbContextFactory()
  {
    var resolved = factory.Services.GetService<IDbContextFactory<KarajobDbContext>>();

    Assert.NotNull(resolved);
  }

  [Fact]
  public async Task Host_StartsWithMigratedSeededInMemoryDatabase()
  {
    var contextFactory = factory.Services.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();

    Assert.Equal(3, await context.Hypotheses.CountAsync());
  }

  [Fact]
  public void Host_ResolvesCurrentUserAsOwner()
  {
    using var scope = factory.Services.CreateScope();

    var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

    Assert.Equal(SeedData.OwnerId, currentUser.UserId);
  }
}
