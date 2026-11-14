using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Cvs;
using Karajob.Application.Files;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Cvs;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Cvs;

public abstract class CvWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  internal InMemoryFileStorage Storage { get; } = new();
  protected CvOptions Options { get; } = new() { MaxFileSizeMb = 5 };
  protected CvService Service { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;

  protected static CvInput Input(string name, IReadOnlyList<string>? tags = null)
  {
    return new CvInput(name, SeedData.RussianLanguageId, tags ?? []);
  }

  protected static CvUpload Upload(string fileName = "cv.pdf", byte[]? bytes = null, string? comment = null)
  {
    var content = bytes ?? [1, 2, 3];
    return new CvUpload(fileName, content.Length, () => new MemoryStream(content), comment);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateCvService(Database, SeedData.OwnerId, Storage, TimeProvider.System, Options);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton<IFileStorage>(Storage);
    Context.Services.AddSingleton(Options);
    Context.Services.AddSingleton(Service);
    Context.Services.AddSingleton(Catalogs);
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected CvWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
  }
}
