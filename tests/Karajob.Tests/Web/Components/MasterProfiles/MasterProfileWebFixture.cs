using Bunit;
using Karajob.Application.MasterProfiles;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.MasterProfiles;

public abstract class MasterProfileWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected MasterProfileService Service { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateMasterProfileService(Database, SeedData.OwnerId, TimeProvider.System);
    Context.Services.AddSingleton(Service);
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected MasterProfileWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
