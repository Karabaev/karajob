using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Hypotheses;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application;
using Karajob.Tests.Infrastructure.Persistence;
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

  protected static VacancyInput Input(
    string url,
    Guid? companyId = null,
    Guid? roleId = null,
    IReadOnlyList<Guid>? hypothesisIds = null)
  {
    return new VacancyInput(url, companyId, roleId, null, null, null, null, null, null, hypothesisIds ?? [], null, null);
  }

  protected async Task<Guid> CreateHypothesisAsync(string name = "Unity Developer RU")
  {
    var hypothesis = await Hypotheses.CreateAsync(new HypothesisInput(
      name,
      SeedData.UnityDeveloperRoleId,
      [],
      [],
      WorkFormat.Remote,
      SeedData.RussianLanguageId,
      null,
      null,
      HypothesisStatus.Draft));
    return hypothesis.Id;
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
    Context.Services.AddSingleton<IKarajobDbContextFactory>(new TestDbContextFactory(Database, SeedData.OwnerId));
    Context.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    Context.Services.AddSingleton(TimeProvider.System);
    Context.Services.AddSingleton(Vacancies);
    Context.Services.AddSingleton(Companies);
    Context.Services.AddSingleton(Hypotheses);
    Context.Services.AddSingleton(Catalogs);
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
  }
}
