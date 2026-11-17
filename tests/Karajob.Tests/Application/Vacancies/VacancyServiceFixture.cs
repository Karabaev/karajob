using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Tracks;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Cvs;
using Karajob.Tests.Application.Tracks;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Vacancies;

public abstract class VacancyServiceFixture : IAsyncLifetime
{
  protected const string DefaultUrl = "https://example.com/jobs/1";

  protected static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  internal FixedTimeProvider Clock { get; } = new(Now);
  internal InMemoryFileStorage Storage { get; } = new();
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected VacancyService Service { get; private set; } = null!;
  protected CompanyService Companies { get; private set; } = null!;
  protected TrackService Tracks { get; private set; } = null!;
  protected CvService Cvs { get; private set; } = null!;

  protected static VacancyInput Input(
    string url = DefaultUrl,
    Guid? companyId = null,
    Guid? roleId = null,
    Guid? locationId = null,
    Guid? languageId = null,
    WorkFormat? workFormat = null,
    long? salaryMin = null,
    long? salaryMax = null,
    string? salaryCurrency = null,
    IReadOnlyList<Guid>? trackIds = null,
    string? description = null,
    string? notes = null,
    IReadOnlyList<Guid>? platformIds = null,
    IReadOnlyList<Guid>? engineIds = null,
    string? title = null)
  {
    return new VacancyInput(
      url,
      title,
      companyId,
      roleId,
      locationId,
      platformIds ?? [],
      engineIds ?? [],
      languageId,
      workFormat,
      salaryMin,
      salaryMax,
      salaryCurrency,
      trackIds ?? [],
      description,
      notes);
  }

  protected VacancyService ServiceFor(Guid userId, TimeZoneInfo? timeZone = null)
  {
    return TestDbContextFactory.CreateVacancyService(Database, userId, Clock, timeZone: timeZone);
  }

  protected async Task<Guid> CreateCompanyAsync(string slug = "pixel-forge")
  {
    var company = await Companies.CreateAsync(new CompanyInput(slug, slug, null, null, [], [], []));
    return company.Id;
  }

  protected async Task<Guid> CreateTrackAsync(
    string name = "Unity Developer RU",
    Guid? userId = null,
    TrackStatus status = TrackStatus.Draft,
    Guid? roleId = null)
  {
    var service = userId is null
      ? Tracks
      : TestDbContextFactory.CreateTrackService(Database, userId.Value, Clock);
    var track = await service.CreateAsync(new TrackInput(
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
      status is TrackStatus.Expanded or TrackStatus.Disabled ? TrackStatus.Draft : status));
    if (status is TrackStatus.Expanded or TrackStatus.Disabled)
    {
      await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
      var stored = await context.Tracks.SingleAsync(h => h.Id == track.Id);
      stored.Status = status;
      await context.SaveChangesAsync();
    }

    return track.Id;
  }

  protected async Task MutateTrackAsync(Guid trackId, Action<Track> mutate)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var track = await context.Tracks.SingleAsync(h => h.Id == trackId);
    mutate(track);
    await context.SaveChangesAsync();
  }

  protected async Task MutateAsync(Guid vacancyId, Action<Vacancy> mutate, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var vacancy = await context.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .SingleAsync(v => v.Id == vacancyId);
    mutate(vacancy);
    await context.SaveChangesAsync();
  }

  protected async Task<Vacancy> LoadAsync(Guid vacancyId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .SingleAsync(v => v.Id == vacancyId);
  }

  protected CvService CvsFor(Guid userId)
  {
    return TestDbContextFactory.CreateCvService(Database, userId, Storage, Clock, new CvOptions { MaxFileSizeMb = 5 });
  }

  protected async Task<(Guid DocumentId, Guid VersionId)> CreateCvAsync(
    string name = "Main CV",
    string fileName = "cv.pdf",
    Guid? userId = null)
  {
    var service = userId is null ? Cvs : CvsFor(userId.Value);
    var details = await service.CreateAsync(new CvInput(name, SeedData.RussianLanguageId, []), MakeUpload(fileName));
    return (details.Id, details.Versions[0].Id);
  }

  protected async Task<Guid> AddCvVersionAsync(Guid documentId, string fileName)
  {
    var details = await Cvs.AddVersionAsync(documentId, MakeUpload(fileName));
    return details.Versions.Single(v => v.FileName == fileName).Id;
  }

  protected async Task<Guid> CreateOnBoardAsync(
    VacancyStage stage,
    Guid trackId,
    string url = DefaultUrl,
    ApplicationOutcome outcome = ApplicationOutcome.Rejected)
  {
    var vacancy = await Service.CreateAsync(Input(url, trackIds: [trackId]));
    var at = vacancy.DiscoveredAt.AddHours(1);
    Guid? cvVersionId = stage is VacancyStage.Applied or VacancyStage.Interviewing or VacancyStage.Closed
      ? (await CreateCvAsync($"CV for {url}", "board.pdf")).VersionId
      : null;
    await MutateAsync(vacancy.Id, v =>
    {
      if (stage == VacancyStage.Inbox) return;

      v.MoveTo(VacancyStage.Targeted, at);
      if (stage == VacancyStage.Targeted) return;

      if (stage == VacancyStage.Preparing)
      {
        v.MoveTo(VacancyStage.Preparing, at);
        return;
      }

      v.Apply(trackId, cvVersionId!.Value, ApplicationChannel.Website, at, null);
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

  private static CvUpload MakeUpload(string fileName)
  {
    var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 1, 2, 3 };
    return new CvUpload(fileName, content.Length, () => new MemoryStream(content), null);
  }

  protected async Task<int> CountVacanciesAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies.CountAsync();
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = ServiceFor(SeedData.OwnerId);
    Companies = TestDbContextFactory.CreateCompanyService(Database, SeedData.OwnerId);
    Tracks = TestDbContextFactory.CreateTrackService(Database, SeedData.OwnerId, Clock);
    Cvs = CvsFor(SeedData.OwnerId);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
