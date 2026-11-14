using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Hypotheses;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Hypotheses;

public abstract class HypothesisWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected HypothesisService Service { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;

  protected static HypothesisInput Input(string name, Guid? roleId = null)
  {
    return new HypothesisInput(
      name,
      roleId ?? SeedData.UnityDeveloperRoleId,
      [],
      [],
      WorkFormat.Remote,
      SeedData.RussianLanguageId,
      null,
      null,
      HypothesisStatus.Draft);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateHypothesisService(Database, SeedData.OwnerId, TimeProvider.System);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Catalogs);
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected HypothesisWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
  }
}
