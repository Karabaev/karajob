using Bunit;
using Karajob.Application.Applications;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Tracks;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Cvs;
using Karajob.Tests.Application.Vacancies;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Components.Common;
using Karajob.Web.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Vacancies;

public abstract class VacancyWebFixture : IAsyncLifetime
{
  protected BunitContext Context { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected VacancyService Vacancies { get; private set; } = null!;
  protected ApplicationService Applications { get; private set; } = null!;
  protected CompanyService Companies { get; private set; } = null!;
  protected TrackService Tracks { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;
  protected CvService Cvs { get; private set; } = null!;
  internal InMemoryFileStorage Storage { get; } = new();
  internal StubPageTitleReader TitleReader { get; } = new();

  protected static VacancyInput Input(
    string url,
    Guid? companyId = null,
    Guid? roleId = null,
    IReadOnlyList<Guid>? trackIds = null,
    string? title = null)
  {
    return new VacancyInput(url, title, companyId, roleId, null, [], [], null, null, null, null, null, trackIds ?? [], null, null);
  }

  protected async Task<Guid> CreateTrackAsync(
    string name = "Unity Developer RU",
    Guid? roleId = null,
    IReadOnlyList<Guid>? platformIds = null,
    WorkFormat workFormat = WorkFormat.Remote,
    Guid? languageId = null)
  {
    var track = await Tracks.CreateAsync(new TrackInput(
      name,
      roleId ?? SeedData.UnityDeveloperRoleId,
      [],
      platformIds ?? [],
      [],
      [],
      workFormat,
      languageId ?? SeedData.RussianLanguageId,
      null,
      null,
      TrackStatus.Draft));
    return track.Id;
  }

  protected async Task<(Guid DocumentId, Guid VersionId)> CreateCvAsync(string name = "Main CV", string fileName = "cv.pdf")
  {
    var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 1, 2, 3 };
    var details = await Cvs.CreateAsync(
      new CvInput(name, SeedData.RussianLanguageId, []),
      new CvUpload(fileName, content.Length, () => new MemoryStream(content), null));
    return (details.Id, details.Versions[0].Id);
  }

  protected async Task SetPrimaryCvAsync(Guid trackId, Guid cvDocumentId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var track = await context.Tracks.SingleAsync(h => h.Id == trackId);
    track.PrimaryCvDocumentId = cvDocumentId;
    await context.SaveChangesAsync();
  }

  protected async Task<Guid> CreateOnBoardAsync(
    VacancyStage stage,
    Guid trackId,
    string url,
    ApplicationOutcome outcome = ApplicationOutcome.Rejected,
    string? coverLetter = null)
  {
    var vacancy = await Vacancies.CreateAsync(Input(url, trackIds: [trackId]));
    var at = vacancy.DiscoveredAt.AddMinutes(1);
    Guid? cvVersionId = stage is VacancyStage.Applied or VacancyStage.Interviewing or VacancyStage.Closed
      ? (await CreateCvAsync($"CV for {url}", "board.pdf")).VersionId
      : null;
    await MutateAsync(vacancy.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, at);
      if (stage == VacancyStage.Targeted) return;

      if (stage == VacancyStage.Preparing)
      {
        v.MoveTo(VacancyStage.Preparing, at);
        return;
      }

      v.Apply(trackId, cvVersionId!.Value, ApplicationChannel.Website, at, coverLetter);
      if (stage == VacancyStage.Interviewing)
      {
        v.MoveTo(VacancyStage.Interviewing, at);
      }
      else if (stage == VacancyStage.Closed)
      {
        v.Close(outcome, at);
      }
    });
    return vacancy.Id;
  }

  protected async Task MutateAsync(Guid vacancyId, Action<Vacancy> mutate)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var vacancy = await context.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .SingleAsync(v => v.Id == vacancyId);
    mutate(vacancy);
    await context.SaveChangesAsync();
  }

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
    Vacancies = TestDbContextFactory.CreateVacancyService(Database, SeedData.OwnerId, TimeProvider.System, TitleReader);
    Applications = TestDbContextFactory.CreateApplicationService(Database, SeedData.OwnerId);
    Companies = TestDbContextFactory.CreateCompanyService(Database, SeedData.OwnerId);
    Tracks = TestDbContextFactory.CreateTrackService(Database, SeedData.OwnerId, TimeProvider.System);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Cvs = TestDbContextFactory.CreateCvService(Database, SeedData.OwnerId, Storage, TimeProvider.System, new CvOptions { MaxFileSizeMb = 5 });
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddScoped<UserDates>();
    Context.Services.AddSingleton(Vacancies);
    Context.Services.AddSingleton(Applications);
    Context.Services.AddSingleton(Companies);
    Context.Services.AddSingleton(Tracks);
    Context.Services.AddSingleton(Catalogs);
    Context.Services.AddSingleton(Cvs);
    Context.Services.AddScoped<InboxNotifier>();
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
    await Database.DisposeAsync();
  }

  protected VacancyWebFixture()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
