using AngleSharp.Dom;
using Bunit;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Profiles;
using Karajob.Web.Components.Layout;
using Karajob.Web.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Layout;

public class MainLayoutTests : IAsyncLifetime
{
  private const string BodyMarker = "body-marker-text";
  private const string DialogTitle = "New vacancy";

  private readonly BunitContext _context = new();
  private SqliteTestDatabase _database = null!;
  private VacancyService _vacancies = null!;

  [Fact]
  public void Render_RendersBody()
  {
    var cut = RenderLayout();

    Assert.Contains(BodyMarker, cut.Markup);
  }

  [Theory]
  [InlineData("inbox", "Inbox")]
  [InlineData("applications", "Applications")]
  [InlineData("companies", "Companies")]
  [InlineData("hypotheses", "Hypotheses")]
  [InlineData("cvs", "CV")]
  [InlineData("settings", "Settings")]
  public void Render_HasNavLink(string href, string text)
  {
    var cut = RenderLayout();

    var link = FindNavLink(cut, text);

    Assert.Equal(href, link.GetAttribute("href")!.Trim('/'));
  }

  [Fact]
  public void Render_HasBoardNavLinkToRoot()
  {
    var cut = RenderLayout();

    var link = FindNavLink(cut, "Board");

    Assert.Equal(string.Empty, link.GetAttribute("href")!.Trim('/'));
  }

  [Fact]
  public void Render_HasSingleAddVacancyButtonWithText()
  {
    var cut = RenderLayout();

    var button = Assert.Single(cut.FindAll(".add-vacancy-button"));
    Assert.Contains("+ Vacancy", button.TextContent);
  }

  [Fact]
  public void Render_HasSingleAddVacancyFab()
  {
    var cut = RenderLayout();

    Assert.Single(cut.FindAll(".add-vacancy-fab"));
  }

  [Fact]
  public void Render_HasProfileMenuActivatorForCurrentProfile()
  {
    var cut = RenderLayout();

    cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button[aria-label='Profile: Owner']")));
  }

  [Fact]
  public void Render_DoesNotShowDialogInitially()
  {
    var cut = RenderLayout();

    Assert.DoesNotContain(DialogTitle, cut.Markup);
  }

  [Fact]
  public void ClickAddVacancyButton_OpensNewVacancyDialog()
  {
    var cut = RenderLayout();

    cut.Find(".add-vacancy-button").Click();

    cut.WaitForAssertion(() => Assert.Contains(DialogTitle, cut.Markup));
  }

  [Fact]
  public void ClickAddVacancyFab_OpensNewVacancyDialog()
  {
    var cut = RenderLayout();

    cut.Find(".add-vacancy-fab").Click();

    cut.WaitForAssertion(() => Assert.Contains(DialogTitle, cut.Markup));
  }

  [Fact]
  public void ClickMenuButton_TogglesDrawer()
  {
    var cut = RenderLayout();
    var wasOpen = IsDrawerOpen(cut);

    cut.Find("button[aria-label='Menu']").Click();

    Assert.NotEqual(wasOpen, IsDrawerOpen(cut));
  }

  [Fact]
  public void Render_EmptyInbox_HasNoInboxCount()
  {
    var cut = RenderLayout();

    Assert.Empty(cut.FindAll(".inbox-count"));
  }

  [Fact]
  public async Task Render_InboxHasVacancies_ShowsInboxCount()
  {
    await _vacancies.CreateAsync(NewVacancy("https://example.com/jobs/1"));
    await _vacancies.CreateAsync(NewVacancy("https://example.com/jobs/2"));

    var cut = RenderLayout();

    cut.WaitForAssertion(() => Assert.Equal("2", cut.Find(".inbox-count").TextContent.Trim()));
  }

  [Fact]
  public async Task NotifyChanged_AfterVacancyAdded_UpdatesInboxCount()
  {
    var cut = RenderLayout();
    Assert.Empty(cut.FindAll(".inbox-count"));
    await _vacancies.CreateAsync(NewVacancy("https://example.com/jobs/1"));

    cut.Services.GetRequiredService<InboxNotifier>().NotifyChanged();

    cut.WaitForAssertion(() => Assert.Equal("1", cut.Find(".inbox-count").TextContent.Trim()));
  }

  [Fact]
  public async Task NotifyChanged_AfterLastVacancyLeavesInbox_RemovesInboxCount()
  {
    var created = await _vacancies.CreateAsync(NewVacancy("https://example.com/jobs/1"));
    var cut = RenderLayout();
    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".inbox-count")));
    await _vacancies.DismissAsync(created.Id, new DismissInput(Karajob.Domain.Vacancies.DismissReason.Other, null));

    cut.Services.GetRequiredService<InboxNotifier>().NotifyChanged();

    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".inbox-count")));
  }

  private static VacancyInput NewVacancy(string url)
  {
    return new VacancyInput(url, null, null, null, null, [], [], null, null, null, null, null, [], null, null);
  }

  private static bool IsDrawerOpen(IRenderedComponent<MainLayout> cut)
  {
    return cut.Find(".mud-drawer").ClassList.Contains("mud-drawer--open");
  }

  private static IElement FindNavLink(IRenderedComponent<MainLayout> cut, string text)
  {
    return cut.FindAll("a").First(a => a.TextContent.Trim() == text);
  }

  private IRenderedComponent<MainLayout> RenderLayout()
  {
    return _context.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, BodyMarker))));
  }

  public async Task InitializeAsync()
  {
    _database = await SqliteTestDatabase.CreateAsync();
    _vacancies = TestDbContextFactory.CreateVacancyService(_database, SeedData.OwnerId, TimeProvider.System);
    _context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(_database, SeedData.OwnerId));
    _context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    _context.Services.AddSingleton(TimeProvider.System);
    _context.Services.AddSingleton(_vacancies);
    _context.Services.AddSingleton(TestDbContextFactory.CreateUserService(_database, SeedData.OwnerId, TimeProvider.System));
    _context.Services.AddSingleton<AntiforgeryStateProvider>(new FakeAntiforgeryStateProvider());
    _context.Services.AddScoped<InboxNotifier>();
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await _context.DisposeAsync();
    await _database.DisposeAsync();
  }

  public MainLayoutTests()
  {
    _context.JSInterop.Mode = JSRuntimeMode.Loose;
    _context.Services.AddMudServices();
    _context.Services.AddLocalization();
  }
}
