using System.Security.Claims;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Tracks;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Components.Authorization;

namespace Karajob.Tests.Web.Users;

public class CurrentUserCircuitHandlerTests : IAsyncLifetime
{
  private SqliteTestDatabase _database = null!;
  private UserService _users = null!;

  public async Task InitializeAsync()
  {
    _database = await SqliteTestDatabase.CreateAsync();
    _users = TestDbContextFactory.CreateUserService(_database, SeedData.OwnerId, new FixedTimeProvider(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc)));
  }

  public async Task DisposeAsync()
  {
    await _database.DisposeAsync();
  }

  [Fact]
  public async Task OnCircuitOpened_AuthenticatedProfile_SetsCurrentUser()
  {
    var profile = await _users.CreateAsync(new UserInput("Anna", "#26a69a"));
    var currentUser = new ProfileCurrentUser();
    var handler = CreateHandler(ProfileAuthentication.CreatePrincipal(profile.Id, "Anna"), currentUser);

    await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

    Assert.Equal(profile.Id, currentUser.UserId);
  }

  [Fact]
  public async Task OnCircuitOpened_AuthenticatedProfile_SetsItsTimeZone()
  {
    var profile = await _users.CreateAsync(new UserInput("Anna", "#26a69a", "Asia/Tokyo"));
    var currentUser = new ProfileCurrentUser();
    var handler = CreateHandler(ProfileAuthentication.CreatePrincipal(profile.Id, "Anna"), currentUser);

    await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

    Assert.Equal(TimeSpan.FromHours(9), currentUser.TimeZone.BaseUtcOffset);
  }

  [Fact]
  public async Task OnCircuitOpened_Anonymous_LeavesNoProfileSelected()
  {
    var currentUser = new ProfileCurrentUser();
    var handler = CreateHandler(new ClaimsPrincipal(new ClaimsIdentity()), currentUser);

    await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

    Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
    Assert.Equal(TimeZoneInfo.Utc, currentUser.TimeZone);
  }

  private CurrentUserCircuitHandler CreateHandler(ClaimsPrincipal user, ProfileCurrentUser currentUser)
  {
    return new CurrentUserCircuitHandler(new FixedAuthenticationStateProvider(user), currentUser, _users);
  }

  private class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      return Task.FromResult(new AuthenticationState(user));
    }
  }
}
