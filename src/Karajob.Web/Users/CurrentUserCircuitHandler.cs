using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Karajob.Web.Users;

public class CurrentUserCircuitHandler : CircuitHandler, IDisposable
{
  private readonly AuthenticationStateProvider _authenticationStateProvider;
  private readonly ProfileCurrentUser _currentUser;

  public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
  {
    _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    await UpdateUserAsync(_authenticationStateProvider.GetAuthenticationStateAsync());
  }

  public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
  {
    return UpdateUserAsync(_authenticationStateProvider.GetAuthenticationStateAsync());
  }

  public void Dispose()
  {
    _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
  }

  private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
  {
    _ = UpdateUserAsync(task);
  }

  private async Task UpdateUserAsync(Task<AuthenticationState> task)
  {
    var state = await task;
    _currentUser.SetUser(state.User);
  }

  public CurrentUserCircuitHandler(AuthenticationStateProvider authenticationStateProvider, ProfileCurrentUser currentUser)
  {
    _authenticationStateProvider = authenticationStateProvider;
    _currentUser = currentUser;
  }
}
