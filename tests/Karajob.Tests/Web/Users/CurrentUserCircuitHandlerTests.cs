using System.Security.Claims;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Components.Authorization;

namespace Karajob.Tests.Web.Users;

public class CurrentUserCircuitHandlerTests
{
  [Fact]
  public async Task OnCircuitOpened_AuthenticatedProfile_SetsCurrentUser()
  {
    var userId = Guid.NewGuid();
    var currentUser = new ProfileCurrentUser();
    var handler = new CurrentUserCircuitHandler(new FixedAuthenticationStateProvider(ProfileAuthentication.CreatePrincipal(userId, "Anna")), currentUser);

    await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

    Assert.Equal(userId, currentUser.UserId);
  }

  [Fact]
  public async Task OnCircuitOpened_Anonymous_LeavesNoProfileSelected()
  {
    var currentUser = new ProfileCurrentUser();
    var handler = new CurrentUserCircuitHandler(new FixedAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())), currentUser);

    await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

    Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
  }

  private class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      return Task.FromResult(new AuthenticationState(user));
    }
  }
}
