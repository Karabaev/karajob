using System.Security.Claims;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class PersistenceCompositionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Fact]
  public void Host_ResolvesDbContextFactory()
  {
    using var scope = factory.Services.CreateScope();

    var resolved = scope.ServiceProvider.GetService<IDbContextFactory<KarajobDbContext>>();

    Assert.NotNull(resolved);
  }

  [Fact]
  public async Task Host_StartsWithMigratedSeededInMemoryDatabase()
  {
    using var scope = factory.Services.CreateScope();
    SetUser(scope, SeedData.OwnerId);
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();

    Assert.Equal(3, await context.Hypotheses.CountAsync());
  }

  [Fact]
  public void Host_ResolvesCurrentUserAsProfileCurrentUser()
  {
    using var scope = factory.Services.CreateScope();

    var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

    Assert.IsType<ProfileCurrentUser>(currentUser);
  }

  [Fact]
  public void Host_FreshScope_CurrentUserIdThrowsInvalidOperationException()
  {
    using var scope = factory.Services.CreateScope();
    var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

    Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
  }

  [Fact]
  public void Host_AfterSetUser_CurrentUserIdReturnsNameIdentifierClaim()
  {
    using var scope = factory.Services.CreateScope();
    var id = Guid.NewGuid();

    SetUser(scope, id);

    Assert.Equal(id, scope.ServiceProvider.GetRequiredService<ICurrentUser>().UserId);
  }

  [Fact]
  public void Host_TwoScopes_HaveIndependentCurrentUsers()
  {
    using var first = factory.Services.CreateScope();
    using var second = factory.Services.CreateScope();
    var firstId = Guid.NewGuid();
    var secondId = Guid.NewGuid();

    SetUser(first, firstId);
    SetUser(second, secondId);

    Assert.Equal(firstId, first.ServiceProvider.GetRequiredService<ICurrentUser>().UserId);
    Assert.Equal(secondId, second.ServiceProvider.GetRequiredService<ICurrentUser>().UserId);
  }

  private static void SetUser(IServiceScope scope, Guid userId)
  {
    var currentUser = Assert.IsType<ProfileCurrentUser>(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    currentUser.SetUser(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test")));
  }
}
