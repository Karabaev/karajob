using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Infrastructure.Persistence;

internal static class PersistenceFixtures
{
  public static DateTime Moment { get; } = new DateTime(2026, 3, 4, 5, 6, 7, 123, DateTimeKind.Utc).AddTicks(4567);

  public static async Task<User> AddUserAsync(SqliteTestDatabase database, string name = "Second")
  {
    var user = new User(name, Moment);
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Users.Add(user);
    await context.SaveChangesAsync();
    return user;
  }

  public static async Task<Company> AddCompanyAsync(SqliteTestDatabase database, string slug)
  {
    var company = new Company(slug, slug);
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Companies.Add(company);
    await context.SaveChangesAsync();
    return company;
  }

  public static Hypothesis NewHypothesis(Guid userId, string name = "Hypo")
  {
    return new Hypothesis(userId, name, "Unity Developer", "en", Moment);
  }

  public static async Task<Hypothesis> AddHypothesisAsync(SqliteTestDatabase database, Guid userId, string name = "Hypo")
  {
    var hypothesis = NewHypothesis(userId, name);
    await using var context = database.CreateContext(userId);
    context.Hypotheses.Add(hypothesis);
    await context.SaveChangesAsync();
    return hypothesis;
  }

  public static async Task<(CvDocument Document, CvVersion Version)> AddCvAsync(SqliteTestDatabase database, Guid userId)
  {
    var document = new CvDocument(userId, "Main CV", "en", Moment);
    var version = document.AddVersion("cv.pdf", "cv/cv.pdf", "application/pdf", 1234, "first", Moment);
    await using var context = database.CreateContext(userId);
    context.CvDocuments.Add(document);
    await context.SaveChangesAsync();
    return (document, version);
  }

  public static async Task<Vacancy> AddVacancyAsync(SqliteTestDatabase database, Guid userId, string url = "https://example.com/jobs/1")
  {
    var vacancy = new Vacancy(userId, url, VacancySource.Manual, Moment);
    await using var context = database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return vacancy;
  }

  public static async Task<Vacancy> AddMatchedVacancyAsync(
    SqliteTestDatabase database,
    Guid userId,
    Guid hypothesisId,
    string url = "https://example.com/jobs/matched")
  {
    var vacancy = new Vacancy(userId, url, VacancySource.Manual, Moment);
    vacancy.SetMatch(hypothesisId, 80, "fits", MatchAuthor.Human);
    await using var context = database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return vacancy;
  }

  public static async Task<(Vacancy Vacancy, Hypothesis Hypothesis, CvVersion CvVersion)> AddAppliedVacancyAsync(
    SqliteTestDatabase database,
    Guid userId,
    string url = "https://example.com/jobs/applied")
  {
    var hypothesis = await AddHypothesisAsync(database, userId);
    var (_, cvVersion) = await AddCvAsync(database, userId);
    var vacancy = new Vacancy(userId, url, VacancySource.Manual, Moment);
    vacancy.SetMatch(hypothesis.Id, 80, "fits", MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Moment);
    vacancy.MoveTo(VacancyStage.Preparing, Moment);
    vacancy.Apply(hypothesis.Id, cvVersion.Id, ApplicationChannel.Website, Moment, "letter");
    await using var context = database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return (vacancy, hypothesis, cvVersion);
  }
}
