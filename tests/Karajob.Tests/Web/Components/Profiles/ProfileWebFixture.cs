using Bunit;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Profiles;

public abstract class ProfileWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected UserService Users { get; private set; } = null!;
  protected ProfileCurrentUser CurrentUser { get; } = new();

  protected async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialogAsync<TDialog>(
    DialogParameters<TDialog> parameters)
    where TDialog : ComponentBase
  {
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var dialogs = Context.Services.GetRequiredService<IDialogService>();
    var reference = await provider.InvokeAsync(() => dialogs.ShowAsync<TDialog>("Dialog", parameters));
    return (provider, reference);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Users = TestDbContextFactory.CreateUserService(Database, SeedData.OwnerId, TimeProvider.System);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton(Users);
    CurrentUser.SetUser(ProfileAuthentication.CreatePrincipal(SeedData.OwnerId, "Owner"));
    Context.Services.AddSingleton(CurrentUser);
    Context.Services.AddSingleton<AntiforgeryStateProvider>(new FakeAntiforgeryStateProvider());
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected ProfileWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
