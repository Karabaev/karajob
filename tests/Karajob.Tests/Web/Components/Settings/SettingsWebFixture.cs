using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Goals;
using Karajob.Application.Persistence;
using Karajob.Application.Transfer;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Settings;

public abstract class SettingsWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected SearchGoalService Service { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;
  protected StubDatabaseTransfer Transfer { get; } = new();

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateSearchGoalService(Database, SeedData.OwnerId);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Catalogs);
    Context.Services.AddSingleton<IDatabaseTransfer>(Transfer);
    Context.Services.AddSingleton(new TransferOptions { MaxImportSizeMb = 1 });
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected SettingsWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
