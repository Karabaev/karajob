using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

internal static class StageTable
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

  public static bool IsAllowed(VacancyStage from, VacancyStage to) => Allowed[from].Contains(to);

  public static IEnumerable<object[]> AllPairs()
  {
    foreach (var from in Enum.GetValues<VacancyStage>())
    {
      foreach (var to in Enum.GetValues<VacancyStage>())
      {
        yield return [from, to];
      }
    }
  }

  public static IEnumerable<object[]> AllowedPairs() =>
    AllPairs().Where(p => IsAllowed((VacancyStage)p[0], (VacancyStage)p[1]));

  public static IEnumerable<object[]> DisallowedPairs() =>
    AllPairs().Where(p => !IsAllowed((VacancyStage)p[0], (VacancyStage)p[1]));

  public static IEnumerable<object[]> StagesWhere(Func<VacancyStage, bool> predicate) =>
    Enum.GetValues<VacancyStage>().Where(predicate).Select(s => new object[] { s });
}
