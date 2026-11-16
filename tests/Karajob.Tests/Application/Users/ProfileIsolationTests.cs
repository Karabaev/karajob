using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Hypotheses;
using Karajob.Application.Vacancies;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Cvs;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Users;

public class ProfileIsolationTests : IAsyncLifetime
{
  private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
  private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 1, 2, 3];

  private readonly FixedTimeProvider _clock = new(Now);
  private readonly InMemoryFileStorage _storage = new();
  private readonly CvOptions _cvOptions = new() { MaxFileSizeMb = 5 };
  private SqliteTestDatabase _database = null!;
  private Guid _otherId;

  [Fact]
  public async Task Vacancies_CreatedByOwner_AreInvisibleToOtherProfile()
  {
    var ownerVacancies = VacanciesFor(SeedData.OwnerId);
    var created = await ownerVacancies.CreateAsync(NewVacancy("https://example.com/jobs/1"));

    Assert.Empty(await VacanciesFor(_otherId).ListAsync(VacancyStage.Inbox));
    Assert.Null(await VacanciesFor(_otherId).FindAsync(created.Id));
    Assert.Single(await ownerVacancies.ListAsync(VacancyStage.Inbox));
  }

  [Fact]
  public async Task Vacancies_CreatedByOtherProfile_AreInvisibleToOwner()
  {
    await VacanciesFor(_otherId).CreateAsync(NewVacancy("https://example.com/jobs/2"));

    Assert.Empty(await VacanciesFor(SeedData.OwnerId).ListAsync(VacancyStage.Inbox));
    Assert.Equal(0, await VacanciesFor(SeedData.OwnerId).CountAsync(VacancyStage.Inbox));
  }

  [Fact]
  public async Task Hypotheses_CreatedByOwner_AreInvisibleToOtherProfile()
  {
    var ownerHypotheses = HypothesesFor(SeedData.OwnerId);
    var created = await ownerHypotheses.CreateAsync(NewHypothesis("Owner hypothesis"));

    Assert.Empty(await HypothesesFor(_otherId).ListAsync());
    Assert.Null(await HypothesesFor(_otherId).FindAsync(created.Id));
    Assert.Single(await ownerHypotheses.ListAsync());
  }

  [Fact]
  public async Task Cvs_CreatedByOwner_AreInvisibleToOtherProfile()
  {
    var ownerCvs = CvsFor(SeedData.OwnerId);
    var created = await ownerCvs.CreateAsync(new CvInput("Owner CV", SeedData.RussianLanguageId, []), Upload());

    Assert.Empty(await CvsFor(_otherId).ListAsync());
    Assert.Null(await CvsFor(_otherId).FindAsync(created.Id));
    Assert.Null(await CvsFor(_otherId).OpenVersionAsync(created.Versions[0].Id));
    Assert.Single(await ownerCvs.ListAsync());
  }

  [Fact]
  public async Task CompanyNotes_SavedByOwner_AreInvisibleToOtherProfile()
  {
    var ownerCompanies = CompaniesFor(SeedData.OwnerId);
    var company = await ownerCompanies.CreateAsync(NewCompany("pixel-forge", "Pixel Forge"));
    await ownerCompanies.SaveNoteAsync(company.Id, "private note", true);

    var seenByOther = await CompaniesFor(_otherId).FindBySlugAsync("pixel-forge");

    Assert.NotNull(seenByOther);
    Assert.Null(seenByOther.Note);
    Assert.False(seenByOther.IsBlacklisted);
    var seenByOwner = await ownerCompanies.FindBySlugAsync("pixel-forge");
    Assert.Equal("private note", seenByOwner!.Note);
    Assert.True(seenByOwner.IsBlacklisted);
  }

  [Fact]
  public async Task Companies_CreatedByOwner_AreSharedWithOtherProfile()
  {
    var company = await CompaniesFor(SeedData.OwnerId).CreateAsync(NewCompany("pixel-forge", "Pixel Forge"));

    var found = await CompaniesFor(_otherId).FindBySlugAsync("pixel-forge");

    Assert.Equal(company.Id, found!.Id);
    Assert.Contains(await CompaniesFor(_otherId).SearchAsync(null), c => c.Id == company.Id);
  }

  [Fact]
  public async Task Projects_AddedByOwner_AreSharedWithOtherProfile()
  {
    var ownerCompanies = CompaniesFor(SeedData.OwnerId);
    var company = await ownerCompanies.CreateAsync(NewCompany("pixel-forge", "Pixel Forge"));
    await ownerCompanies.AddProjectAsync(company.Id, new ProjectInput("Dragon Quest", null, null, Karajob.Domain.Companies.ProjectStatus.InDevelopment, null));

    var found = await CompaniesFor(_otherId).FindBySlugAsync("pixel-forge");

    Assert.Equal("Dragon Quest", Assert.Single(found!.Projects).Name);
  }

  [Theory]
  [InlineData(CatalogKind.Role, null)]
  [InlineData(CatalogKind.Location, null)]
  [InlineData(CatalogKind.Language, "zz")]
  public async Task Catalogs_EntryCreatedByOwner_IsSharedWithOtherProfile(CatalogKind kind, string? code)
  {
    var created = await TestDbContextFactory.CreateCatalogService(_database, SeedData.OwnerId)
      .CreateAsync(kind, new CatalogInput("Shared entry", code));

    var listed = await TestDbContextFactory.CreateCatalogService(_database, _otherId).ListAsync(kind);

    Assert.Contains(listed, e => e.Id == created.Id);
  }

  public async Task InitializeAsync()
  {
    _database = await SqliteTestDatabase.CreateAsync();
    _otherId = (await PersistenceFixtures.AddUserAsync(_database)).Id;
  }

  public async Task DisposeAsync()
  {
    await _database.DisposeAsync();
  }

  private static VacancyInput NewVacancy(string url)
  {
    return new VacancyInput(url, null, null, null, null, [], [], null, null, null, null, null, [], null, null);
  }

  private static HypothesisInput NewHypothesis(string name)
  {
    return new HypothesisInput(name, SeedData.UnityDeveloperRoleId, [], [], [], [], WorkFormat.Remote, SeedData.RussianLanguageId, null, null, HypothesisStatus.Draft);
  }

  private static CompanyInput NewCompany(string slug, string name)
  {
    return new CompanyInput(slug, name, null, null, [], []);
  }

  private static CvUpload Upload()
  {
    return new CvUpload("cv.pdf", PdfBytes.Length, () => new MemoryStream(PdfBytes), null);
  }

  private VacancyService VacanciesFor(Guid userId)
  {
    return TestDbContextFactory.CreateVacancyService(_database, userId, _clock);
  }

  private HypothesisService HypothesesFor(Guid userId)
  {
    return TestDbContextFactory.CreateHypothesisService(_database, userId, _clock);
  }

  private CvService CvsFor(Guid userId)
  {
    return TestDbContextFactory.CreateCvService(_database, userId, _storage, _clock, _cvOptions);
  }

  private CompanyService CompaniesFor(Guid userId)
  {
    return TestDbContextFactory.CreateCompanyService(_database, userId);
  }
}
