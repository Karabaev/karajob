using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;

namespace Karajob.Infrastructure.Persistence;

public static class DevelopmentSeed
{
  public static async Task SeedAsync(KarajobDbContext context, DateTime utcNow, CancellationToken cancellationToken = default)
  {
    var userId = SeedData.OwnerId;
    var start = utcNow.AddDays(-30);

    var unityRu = CreateTrack(userId, "Unity Developer RU", SeedData.UnityDeveloperRoleId, SeedData.RussianLanguageId, WorkFormat.Remote, start);
    var unrealRu = CreateTrack(userId, "Unreal Developer RU", SeedData.UnrealDeveloperRoleId, SeedData.RussianLanguageId, WorkFormat.Remote, start);
    var unityEn = CreateTrack(userId, "Unity Developer EN", SeedData.UnityDeveloperRoleId, SeedData.EnglishLanguageId, WorkFormat.Remote | WorkFormat.Relocation, start);
    unityEn.SetLocations([SeedData.EuLocationId]);
    context.Tracks.AddRange(unityRu, unrealRu, unityEn);

    var cv = new CvDocument(userId, "Unity CV", SeedData.RussianLanguageId, start);
    var cvVersion = cv.AddVersion("unity-cv.pdf", "seed/unity-cv.pdf", "application/pdf", 120_000, "Seed version", start);
    context.CvDocuments.Add(cv);

    var pixel = CreateCompany("pixel-forge", "Pixel Forge", "pixelforge.example");
    var northern = CreateCompany("northern-games", "Northern Games", "northerngames.example");
    var orbit = CreateCompany("orbit-studio", "Orbit Studio", "orbitstudio.example");
    context.Companies.AddRange(pixel, northern, orbit);

    var day = 0;

    Vacancy NewVacancy(Company? company, Guid? roleId)
    {
      day++;
      var vacancy = new Vacancy(userId, $"https://jobs.example/{day}", VacancySource.Manual, start.AddDays(day))
      {
        CompanyId = company?.Id,
        RoleId = roleId,
      };
      context.Vacancies.Add(vacancy);
      return vacancy;
    }

    DateTime At(int offset)
    {
      return start.AddDays(day + offset);
    }

    NewVacancy(null, SeedData.UnityDeveloperRoleId);
    NewVacancy(orbit, null);

    foreach (var (company, track) in new[] { (pixel, unityRu), (northern, unrealRu) })
    {
      var targeted = NewVacancy(company, track.RoleId);
      targeted.SetMatch(track.Id, 70, null, MatchAuthor.Human);
      targeted.MoveTo(VacancyStage.Targeted, At(1));
    }

    var preparing = NewVacancy(orbit, SeedData.UnityDeveloperRoleId);
    preparing.SetMatch(unityEn.Id, 80, null, MatchAuthor.Human);
    preparing.MoveTo(VacancyStage.Targeted, At(1));
    preparing.MoveTo(VacancyStage.Preparing, At(2));

    foreach (var company in new[] { pixel, northern })
    {
      Apply(NewVacancy(company, SeedData.UnityDeveloperRoleId), unityRu.Id, cvVersion.Id, At(1), At(2));
    }

    var interviewing = NewVacancy(orbit, SeedData.UnityDeveloperRoleId);
    Apply(interviewing, unityEn.Id, cvVersion.Id, At(1), At(2));
    interviewing.MoveTo(VacancyStage.Interviewing, At(5));

    var closed = NewVacancy(pixel, SeedData.UnityDeveloperRoleId);
    Apply(closed, unityRu.Id, cvVersion.Id, At(1), At(2));
    closed.Close(ApplicationOutcome.Rejected, At(7));

    var dismissed = NewVacancy(northern, SeedData.UnrealDeveloperRoleId);
    dismissed.SetMatch(unrealRu.Id, 30, "Art-heavy role", MatchAuthor.Human);
    dismissed.Dismiss(DismissReason.WrongRole, "Mostly art, little code", At(1));

    await context.SaveChangesAsync(cancellationToken);
  }

  private static void Apply(Vacancy vacancy, Guid trackId, Guid cvVersionId, DateTime targetedAt, DateTime appliedAt)
  {
    vacancy.SetMatch(trackId, 75, null, MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, targetedAt);
    vacancy.Apply(trackId, cvVersionId, ApplicationChannel.Website, appliedAt, null);
  }

  private static Track CreateTrack(
    Guid userId,
    string name,
    Guid roleId,
    Guid languageId,
    WorkFormat workFormat,
    DateTime startedAt)
  {
    var track = new Track(userId, name, roleId, languageId, startedAt)
    {
      WorkFormat = workFormat,
      Status = TrackStatus.Active,
      StartedAt = startedAt,
    };
    track.SetPlatforms([SeedData.PcPlatformId]);
    return track;
  }

  private static Company CreateCompany(string slug, string name, string domain)
  {
    var company = new Company(slug, name) { Website = $"https://{domain}" };
    company.AddDomain(domain);
    return company;
  }
}
