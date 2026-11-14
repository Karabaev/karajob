using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Catalogs;

public abstract class CatalogWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected CatalogService Service { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(Service);
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected CatalogWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
  }
}
