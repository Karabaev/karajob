namespace Karajob.Domain.Vacancies;

public static class VacancyStageRules
{
  private static readonly Dictionary<VacancyStage, VacancyStage[]> Allowed = new()
  {
    [VacancyStage.Inbox] = [VacancyStage.Targeted, VacancyStage.Dismissed],
    [VacancyStage.Targeted] = [VacancyStage.Preparing, VacancyStage.Applied, VacancyStage.Dismissed],
    [VacancyStage.Preparing] = [VacancyStage.Targeted, VacancyStage.Applied, VacancyStage.Dismissed],
    [VacancyStage.Applied] = [VacancyStage.Interviewing, VacancyStage.Closed],
    [VacancyStage.Interviewing] = [VacancyStage.Closed],
    [VacancyStage.Closed] = [VacancyStage.Applied, VacancyStage.Interviewing],
    [VacancyStage.Dismissed] = [VacancyStage.Inbox, VacancyStage.Targeted, VacancyStage.Preparing],
  };

  public static bool CanMove(VacancyStage from, VacancyStage to)
  {
    return Allowed[from].Contains(to);
  }
}
