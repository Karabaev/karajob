using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;

namespace Karajob.Infrastructure.Persistence;

public static class DevelopmentSeed
{
  public static async Task SeedAsync(KarajobDbContext context, DateTime utcNow, CancellationToken cancellationToken = default)
  {
    var userId = SeedData.OwnerId;
    var start = utcNow.AddDays(-30);

    var unityRu = CreateHypothesis(userId, "Unity RU", "Unity Developer", "ru", WorkFormat.Remote, start);
    var unrealRu = CreateHypothesis(userId, "Unreal RU", "Unreal Developer", "ru", WorkFormat.Remote, start);
    var unityEn = CreateHypothesis(userId, "Unity EN", "Unity Developer", "en", WorkFormat.Remote | WorkFormat.Relocation, start);
    unityEn.Locations.Add("EU");
    context.Hypotheses.AddRange(unityRu, unrealRu, unityEn);

    var cv = new CvDocument(userId, "Unity CV", "ru", start);
    var cvVersion = cv.AddVersion("unity-cv.pdf", "seed/unity-cv.pdf", "application/pdf", 120_000, "Seed version", start);
    context.CvDocuments.Add(cv);

    var pixel = CreateCompany("pixel-forge", "Pixel Forge", "pixelforge.example");
    var northern = CreateCompany("northern-games", "Northern Games", "northerngames.example");
    var orbit = CreateCompany("orbit-studio", "Orbit Studio", "orbitstudio.example");
    context.Companies.AddRange(pixel, northern, orbit);

    var day = 0;

    Vacancy NewVacancy(Company? company, string role)
    {
      day++;
      var vacancy = new Vacancy(userId, $"https://jobs.example/{day}", VacancySource.Manual, start.AddDays(day))
      {
        CompanyId = company?.Id,
        Role = role,
      };
      context.Vacancies.Add(vacancy);
      return vacancy;
    }

    DateTime At(int offset)
    {
      return start.AddDays(day + offset);
    }

    NewVacancy(null, "Unity Developer");
    NewVacancy(orbit, "Gameplay Programmer");

    foreach (var (company, hypothesis) in new[] { (pixel, unityRu), (northern, unrealRu) })
    {
      var targeted = NewVacancy(company, hypothesis.Role);
      targeted.SetMatch(hypothesis.Id, 70, null, MatchAuthor.Human);
      targeted.MoveTo(VacancyStage.Targeted, At(1));
    }

    var preparing = NewVacancy(orbit, "Senior Unity Developer");
    preparing.SetMatch(unityEn.Id, 80, null, MatchAuthor.Human);
    preparing.MoveTo(VacancyStage.Targeted, At(1));
    preparing.MoveTo(VacancyStage.Preparing, At(2));

    foreach (var company in new[] { pixel, northern })
    {
      Apply(NewVacancy(company, "Unity Developer"), unityRu.Id, cvVersion.Id, At(1), At(2));
    }

    var interviewing = NewVacancy(orbit, "Unity Developer");
    Apply(interviewing, unityEn.Id, cvVersion.Id, At(1), At(2));
    interviewing.MoveTo(VacancyStage.Interviewing, At(5));

    var closed = NewVacancy(pixel, "Unity Tools Developer");
    Apply(closed, unityRu.Id, cvVersion.Id, At(1), At(2));
    closed.Close(ApplicationOutcome.Rejected, At(7));

    var dismissed = NewVacancy(northern, "Unreal Technical Artist");
    dismissed.SetMatch(unrealRu.Id, 30, "Art-heavy role", MatchAuthor.Human);
    dismissed.Dismiss(DismissReason.WrongRole, "Mostly art, little code", At(1));

    await context.SaveChangesAsync(cancellationToken);
  }

  private static void Apply(Vacancy vacancy, Guid hypothesisId, Guid cvVersionId, DateTime targetedAt, DateTime appliedAt)
  {
    vacancy.SetMatch(hypothesisId, 75, null, MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, targetedAt);
    vacancy.Apply(hypothesisId, cvVersionId, ApplicationChannel.Website, appliedAt, null);
  }

  private static Hypothesis CreateHypothesis(
    Guid userId,
    string name,
    string role,
    string language,
    WorkFormat workFormat,
    DateTime startedAt)
  {
    return new Hypothesis(userId, name, role, language, startedAt)
    {
      WorkFormat = workFormat,
      Status = HypothesisStatus.Active,
      StartedAt = startedAt,
    };
  }

  private static Company CreateCompany(string slug, string name, string domain)
  {
    var company = new Company(slug, name) { Website = $"https://{domain}" };
    company.AddDomain(domain);
    return company;
  }
}
