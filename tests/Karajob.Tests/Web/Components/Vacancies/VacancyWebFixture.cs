using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Hypotheses;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Cvs;
using Karajob.Tests.Infrastructure.Persistence;
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
  protected CompanyService Companies { get; private set; } = null!;
  protected HypothesisService Hypotheses { get; private set; } = null!;
  protected CatalogService Catalogs { get; private set; } = null!;
  protected CvService Cvs { get; private set; } = null!;
  internal InMemoryFileStorage Storage { get; } = new();

  protected static VacancyInput Input(
    string url,
    Guid? companyId = null,
    Guid? roleId = null,
    IReadOnlyList<Guid>? hypothesisIds = null)
  {
    return new VacancyInput(url, companyId, roleId, null, [], [], null, null, null, null, null, hypothesisIds ?? [], null, null);
  }

  protected async Task<Guid> CreateHypothesisAsync(string name = "Unity Developer RU", Guid? roleId = null)
  {
    var hypothesis = await Hypotheses.CreateAsync(new HypothesisInput(
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
      HypothesisStatus.Draft));
    return hypothesis.Id;
  }

  protected async Task<(Guid DocumentId, Guid VersionId)> CreateCvAsync(string name = "Main CV", string fileName = "cv.pdf")
  {
    var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 1, 2, 3 };
    var details = await Cvs.CreateAsync(
      new CvInput(name, SeedData.RussianLanguageId, []),
      new CvUpload(fileName, content.Length, () => new MemoryStream(content), null));
    return (details.Id, details.Versions[0].Id);
  }

  protected async Task SetPrimaryCvAsync(Guid hypothesisId, Guid cvDocumentId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var hypothesis = await context.Hypotheses.SingleAsync(h => h.Id == hypothesisId);
    hypothesis.PrimaryCvDocumentId = cvDocumentId;
    await context.SaveChangesAsync();
  }

  protected async Task<Guid> CreateOnBoardAsync(
    VacancyStage stage,
    Guid hypothesisId,
    string url,
    ApplicationOutcome outcome = ApplicationOutcome.Rejected,
    string? coverLetter = null)
  {
    var vacancy = await Vacancies.CreateAsync(Input(url, hypothesisIds: [hypothesisId]));
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

      v.Apply(hypothesisId, cvVersionId!.Value, ApplicationChannel.Website, at, coverLetter);
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
    Vacancies = TestDbContextFactory.CreateVacancyService(Database, SeedData.OwnerId, TimeProvider.System);
    Companies = TestDbContextFactory.CreateCompanyService(Database, SeedData.OwnerId);
    Hypotheses = TestDbContextFactory.CreateHypothesisService(Database, SeedData.OwnerId, TimeProvider.System);
    Catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    Cvs = TestDbContextFactory.CreateCvService(Database, SeedData.OwnerId, Storage, TimeProvider.System, new CvOptions { MaxFileSizeMb = 5 });
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton(Vacancies);
    Context.Services.AddSingleton(Companies);
    Context.Services.AddSingleton(Hypotheses);
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
