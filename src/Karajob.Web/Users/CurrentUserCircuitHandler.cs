using Karajob.Application.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Karajob.Web.Users;

public class CurrentUserCircuitHandler(
  AuthenticationStateProvider authenticationStateProvider,
  ProfileCurrentUser currentUser,
  UserService users)
  : CircuitHandler
{
  // A profile switch is a full page reload with a new circuit, so the user is fixed for the circuit's lifetime.
  // The circuit has its own DI scope, separate from the prerender request, so the time zone is loaded here too.
  public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
  {
    var state = await authenticationStateProvider.GetAuthenticationStateAsync();
    currentUser.SetUser(state.User);
    if (ProfileAuthentication.GetUserId(state.User) is not { } userId) return;

    currentUser.SetTimeZone(await users.GetTimeZoneAsync(userId, cancellationToken));
  }
}
