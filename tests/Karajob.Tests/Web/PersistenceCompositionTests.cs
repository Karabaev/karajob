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
  public void Host_ResolvesCurrentUserAsOwner()
  {
    using var scope = factory.Services.CreateScope();

    var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

    Assert.Equal(SeedData.OwnerId, currentUser.UserId);
  }
}
