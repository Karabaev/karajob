using Karajob.Web.Components.Common;
using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Tracks;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Tracks;

public abstract class TrackWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected TrackService Service { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;
  protected TrackDecisionService Decisions { get; private set; } = null!;

  protected static TrackInput Input(string name, Guid? roleId = null)
  {
    return new TrackInput(
      name,
      roleId ?? SeedData.UnityDeveloperRoleId,
      [],
      [],
      [],
      [],
      WorkFormat.Remote,
      SeedData.RussianLanguageId,
      null,
      null,
      TrackStatus.Draft);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateTrackService(Database, SeedData.OwnerId, TimeProvider.System);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Decisions = TestDbContextFactory.CreateTrackDecisionService(Database, SeedData.OwnerId, TimeProvider.System);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddScoped<UserDates>();
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Catalogs);
    Context.Services.AddSingleton(Decisions);
    Context.Services.AddSingleton(TestDbContextFactory.CreateTrackMetricsService(Database, SeedData.OwnerId, TimeProvider.System));
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected TrackWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
