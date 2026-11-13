using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class RoundTripTests
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task User_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var user = new User("Alice", Moment);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Users.Add(user);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Users.SingleAsync(u => u.Id == user.Id);

    Assert.Equal("Alice", loaded.Name);
    AssertUtc(Moment, loaded.CreatedAt);
  }

  [Fact]
  public async Task Company_SavedAndReloaded_PreservesDomainsAliasesAndFields()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = new Company("acme-games", "Acme Games")
    {
      Website = "https://acme.example.com",
      Description = "Makes games",
      IconPath = "icons/acme.png",
    };
    company.AddDomain("acme.example.com");
    company.AddDomain("acme.io");
    company.AddAlias("Acme");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(company);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Companies.Include(c => c.Domains).Include(c => c.Aliases).SingleAsync(c => c.Id == company.Id);

    Assert.Equal("acme-games", loaded.Slug);
    Assert.Equal("Acme Games", loaded.Name);
    Assert.Equal(company.Website, loaded.Website);
    Assert.Equal("Makes games", loaded.Description);
    Assert.Equal("icons/acme.png", loaded.IconPath);
    Assert.Equal(company.Domains.Select(d => d.Value).Order(), loaded.Domains.Select(d => d.Value).Order());
    Assert.Equal(company.Aliases.Select(a => a.Value).Order(), loaded.Aliases.Select(a => a.Value).Order());
  }

  [Fact]
  public async Task Project_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = await PersistenceFixtures.AddCompanyAsync(database, "proj-co");
    var project = new Project(company.Id, "Space Game", ProjectStatus.InDevelopment)
    {
      Genre = "Strategy",
      Engine = "Unity",
      Url = "https://game.example.com",
    };
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Projects.Add(project);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Projects.SingleAsync(p => p.Id == project.Id);

    Assert.Equal(company.Id, loaded.CompanyId);
    Assert.Equal("Space Game", loaded.Name);
    Assert.Equal("Strategy", loaded.Genre);
    Assert.Equal("Unity", loaded.Engine);
    Assert.Equal(ProjectStatus.InDevelopment, loaded.Status);
    Assert.Equal(project.Url, loaded.Url);
  }

  [Fact]
  public async Task UserCompanyNote_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = await PersistenceFixtures.AddCompanyAsync(database, "note-co");
    var note = new UserCompanyNote(SeedData.OwnerId, company.Id) { Note = "Nice team", IsBlacklisted = true };
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.UserCompanyNotes.Add(note);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.UserCompanyNotes.SingleAsync(n => n.Id == note.Id);

    Assert.Equal(SeedData.OwnerId, loaded.UserId);
    Assert.Equal(company.Id, loaded.CompanyId);
    Assert.Equal("Nice team", loaded.Note);
    Assert.True(loaded.IsBlacklisted);
  }

  [Fact]
  public async Task Hypothesis_SavedAndReloaded_PreservesListsFlagsAndDates()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (cv, _) = await PersistenceFixtures.AddCvAsync(database, SeedData.OwnerId);
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.Locations.AddRange(["Berlin", "Lisbon"]);
    hypothesis.Industries.AddRange(["Games", "Fintech"]);
    hypothesis.WorkFormat = WorkFormat.Remote | WorkFormat.Relocation;
    hypothesis.Positioning = "Senior";
    hypothesis.StartedAt = Moment.AddDays(1);
    hypothesis.Status = HypothesisStatus.Active;
    hypothesis.PrimaryCvDocumentId = cv.Id;
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Hypotheses.Add(hypothesis);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);

    Assert.Equal(hypothesis.Name, loaded.Name);
    Assert.Equal("Unity Developer", loaded.Role);
    Assert.Equal("en", loaded.Language);
    Assert.Equal("Senior", loaded.Positioning);
    Assert.Equal(["Berlin", "Lisbon"], loaded.Locations);
    Assert.Equal(["Games", "Fintech"], loaded.Industries);
    Assert.Equal(WorkFormat.Remote | WorkFormat.Relocation, loaded.WorkFormat);
    Assert.Equal(HypothesisStatus.Active, loaded.Status);
    Assert.Equal(cv.Id, loaded.PrimaryCvDocumentId);
    AssertUtc(Moment, loaded.CreatedAt);
    AssertUtc(Moment.AddDays(1), loaded.StartedAt);
  }

  [Fact]
  public async Task Hypothesis_WithoutOptionalValues_ReloadsNullsAndEmptyLists()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);

    Assert.Null(loaded.StartedAt);
    Assert.Null(loaded.PrimaryCvDocumentId);
    Assert.Empty(loaded.Locations);
    Assert.Empty(loaded.Industries);
  }

  [Fact]
  public async Task CvDocument_SavedAndReloaded_PreservesVersion()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (document, version) = await PersistenceFixtures.AddCvAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loadedDocument = await fresh.CvDocuments.Include(d => d.Versions).SingleAsync(d => d.Id == document.Id);
    var loadedVersion = Assert.Single(loadedDocument.Versions);

    Assert.Equal("Main CV", loadedDocument.Name);
    Assert.Equal("en", loadedDocument.Language);
    AssertUtc(Moment, loadedDocument.CreatedAt);
    Assert.Equal(version.Id, loadedVersion.Id);
    Assert.Equal(document.Id, loadedVersion.CvDocumentId);
    Assert.Equal(SeedData.OwnerId, loadedVersion.UserId);
    Assert.Equal("cv.pdf", loadedVersion.FileName);
    Assert.Equal("cv/cv.pdf", loadedVersion.StoragePath);
    Assert.Equal("application/pdf", loadedVersion.ContentType);
    Assert.Equal(1234, loadedVersion.SizeBytes);
    Assert.Equal("first", loadedVersion.Comment);
    AssertUtc(Moment, loadedVersion.CreatedAt);
  }

  [Fact]
  public async Task Vacancy_SavedAndReloaded_PreservesFieldsAndSalary()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = await PersistenceFixtures.AddCompanyAsync(database, "vac-co");
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/full", VacancySource.AiScan, Moment)
    {
      CompanyId = company.Id,
      Role = "Unity Dev",
      Location = "Berlin",
      WorkFormat = WorkFormat.Hybrid | WorkFormat.OnSite,
      Salary = new SalaryRange(3000, 5000, "eur"),
      Description = "desc",
      Notes = "notes",
      PublishedAt = Moment.AddDays(-2),
    };
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Vacancies.Add(vacancy);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(company.Id, loaded.CompanyId);
    Assert.Equal("Unity Dev", loaded.Role);
    Assert.Equal(vacancy.Url, loaded.Url);
    Assert.Equal("Berlin", loaded.Location);
    Assert.Equal(WorkFormat.Hybrid | WorkFormat.OnSite, loaded.WorkFormat);
    Assert.Equal(3000, loaded.Salary!.Min);
    Assert.Equal(5000, loaded.Salary.Max);
    Assert.Equal("EUR", loaded.Salary.Currency);
    Assert.Equal("desc", loaded.Description);
    Assert.Equal("notes", loaded.Notes);
    Assert.Equal(VacancySource.AiScan, loaded.Source);
    Assert.Equal(VacancyStage.Inbox, loaded.Stage);
    AssertUtc(Moment, loaded.DiscoveredAt);
    AssertUtc(Moment.AddDays(-2), loaded.PublishedAt);
  }

  [Fact]
  public async Task Vacancy_WithOpenEndedSalary_ReloadsNullBound()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/open", VacancySource.Manual, Moment)
    {
      Salary = new SalaryRange(null, 7000, "USD"),
    };
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Vacancies.Add(vacancy);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Null(loaded.Salary!.Min);
    Assert.Equal(7000, loaded.Salary.Max);
  }

  [Fact]
  public async Task Vacancy_WithoutSalary_ReloadsNullSalary()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = await PersistenceFixtures.AddVacancyAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Null(loaded.Salary);
    Assert.Null(loaded.PublishedAt);
    Assert.Null(loaded.WorkFormat);
    Assert.Null(loaded.CompanyId);
  }

  [Fact]
  public async Task VacancyMatch_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/match", VacancySource.Manual, Moment);
    var match = vacancy.SetMatch(hypothesis.Id, 77, "good fit", MatchAuthor.Ai);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Vacancies.Add(vacancy);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.VacancyMatches.SingleAsync(m => m.Id == match.Id);

    Assert.Equal(vacancy.Id, loaded.VacancyId);
    Assert.Equal(hypothesis.Id, loaded.HypothesisId);
    Assert.Equal(77, loaded.Score);
    Assert.Equal("good fit", loaded.Rationale);
    Assert.Equal(MatchAuthor.Ai, loaded.MatchedBy);
  }

  [Fact]
  public async Task JobApplication_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (vacancy, hypothesis, cvVersion) = await PersistenceFixtures.AddAppliedVacancyAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.JobApplications.SingleAsync(a => a.VacancyId == vacancy.Id);

    Assert.Equal(vacancy.Application!.Id, loaded.Id);
    Assert.Equal(hypothesis.Id, loaded.HypothesisId);
    Assert.Equal(cvVersion.Id, loaded.CvVersionId);
    Assert.Equal(ApplicationChannel.Website, loaded.Channel);
    Assert.Equal("letter", loaded.CoverLetter);
    Assert.Null(loaded.Outcome);
    AssertUtc(Moment, loaded.AppliedAt);
  }

  [Fact]
  public async Task ApplicationEvent_SavedAndReloaded_PreservesValues()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (vacancy, _, _) = await PersistenceFixtures.AddAppliedVacancyAsync(database, SeedData.OwnerId);
    var expected = vacancy.Events.Last();

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.ApplicationEvents.SingleAsync(e => e.Id == expected.Id);

    Assert.Equal(vacancy.Id, loaded.VacancyId);
    Assert.Equal(ApplicationEventType.StageChanged, loaded.Type);
    Assert.Equal(VacancyStage.Preparing, loaded.FromStage);
    Assert.Equal(VacancyStage.Applied, loaded.ToStage);
    AssertUtc(Moment, loaded.OccurredAt);
  }

  [Fact]
  public async Task ApplicationEvent_NoteWithoutStages_ReloadsNullStages()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = await PersistenceFixtures.AddVacancyAsync(database, SeedData.OwnerId);
    var note = new ApplicationEvent(SeedData.OwnerId, vacancy.Id, ApplicationEventType.Note, Moment, null, null, "called");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.ApplicationEvents.Add(note);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.ApplicationEvents.SingleAsync(e => e.Id == note.Id);

    Assert.Null(loaded.FromStage);
    Assert.Null(loaded.ToStage);
    Assert.Equal("called", loaded.Note);
  }

  private static void AssertUtc(DateTime expected, DateTime? actual)
  {
    Assert.NotNull(actual);
    Assert.Equal(DateTimeKind.Utc, actual.Value.Kind);
    Assert.Equal(expected, actual.Value);
  }
}
