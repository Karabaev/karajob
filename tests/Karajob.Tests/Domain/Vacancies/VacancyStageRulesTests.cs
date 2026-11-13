using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class VacancyStageRulesTests
{
  public static IEnumerable<object[]> Pairs() => StageTable.AllPairs();

  [Theory]
  [MemberData(nameof(Pairs))]
  public void CanMove_Pair_MatchesTransitionTable(VacancyStage from, VacancyStage to)
  {
    Assert.Equal(StageTable.IsAllowed(from, to), VacancyStageRules.CanMove(from, to));
  }
}
