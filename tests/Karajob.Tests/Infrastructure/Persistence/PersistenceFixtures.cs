using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Tracks;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Infrastructure.Persistence;

internal static class PersistenceFixtures
{
  public static DateTime Moment { get; } = new DateTime(2026, 3, 4, 5, 6, 7, 123, DateTimeKind.Utc).AddTicks(4567);

  public static async Task<User> AddUserAsync(SqliteTestDatabase database, string name = "Second")
  {
    var user = new User(name, "#26a69a", Moment);
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

  public static Track NewTrack(Guid userId, string name = "Hypo")
  {
    return new Track(userId, name, SeedData.UnityDeveloperRoleId, SeedData.EnglishLanguageId, Moment);
  }

  public static async Task<Track> AddTrackAsync(SqliteTestDatabase database, Guid userId, string name = "Hypo")
  {
    var track = NewTrack(userId, name);
    await using var context = database.CreateContext(userId);
    context.Tracks.Add(track);
    await context.SaveChangesAsync();
    return track;
  }

  public static async Task<(CvDocument Document, CvVersion Version)> AddCvAsync(SqliteTestDatabase database, Guid userId)
  {
    var document = new CvDocument(userId, "Main CV", SeedData.EnglishLanguageId, Moment);
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
    Guid trackId,
    string url = "https://example.com/jobs/matched")
  {
    var vacancy = new Vacancy(userId, url, VacancySource.Manual, Moment);
    vacancy.SetMatch(trackId, 80, "fits", MatchAuthor.Human);
    await using var context = database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return vacancy;
  }

  public static async Task<(Vacancy Vacancy, Track Track, CvVersion CvVersion)> AddAppliedVacancyAsync(
    SqliteTestDatabase database,
    Guid userId,
    string url = "https://example.com/jobs/applied")
  {
    var track = await AddTrackAsync(database, userId);
    var (_, cvVersion) = await AddCvAsync(database, userId);
    var vacancy = new Vacancy(userId, url, VacancySource.Manual, Moment);
    vacancy.SetMatch(track.Id, 80, "fits", MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Moment);
    vacancy.MoveTo(VacancyStage.Preparing, Moment);
    vacancy.Apply(track.Id, cvVersion.Id, ApplicationChannel.Website, Moment, "letter");
    await using var context = database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return (vacancy, track, cvVersion);
  }
}
