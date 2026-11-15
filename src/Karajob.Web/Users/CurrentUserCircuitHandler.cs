using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Karajob.Web.Users;

public class CurrentUserCircuitHandler(AuthenticationStateProvider authenticationStateProvider, ProfileCurrentUser currentUser)
  : CircuitHandler
{
  // A profile switch is a full page reload with a new circuit, so the user is fixed for the circuit's lifetime.
  public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
  {
    var state = await authenticationStateProvider.GetAuthenticationStateAsync();
    currentUser.SetUser(state.User);
  }
}
