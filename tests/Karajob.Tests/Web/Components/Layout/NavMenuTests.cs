using Bunit;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Components.Layout;
using Karajob.Web.Vacancies;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Layout;

public class NavMenuTests : IAsyncLifetime
{
  private readonly BunitContext _context = new();
  private SqliteTestDatabase _database = null!;

  [Fact]
  public void Render_HasNoLinkToMasterProfile()
  {
    var cut = _context.Render<NavMenu>();

    var hrefs = cut.FindAll("a").Select(a => a.GetAttribute("href")!.Trim('/')).ToList();
    Assert.Contains("companies", hrefs);
    Assert.DoesNotContain(hrefs, h => h.EndsWith("master-profile"));
  }

  public async Task InitializeAsync()
  {
    _database = await SqliteTestDatabase.CreateAsync();
    _context.Services.AddSingleton(TestDbContextFactory.CreateVacancyService(_database, SeedData.OwnerId, TimeProvider.System));
    _context.Services.AddScoped<InboxNotifier>();
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await _context.DisposeAsync();
    await _database.DisposeAsync();
  }

  public NavMenuTests()
  {
    _context.JSInterop.Mode = JSRuntimeMode.Loose;
    _context.Services.AddMudServices();
    _context.Services.AddLocalization();
  }
}
