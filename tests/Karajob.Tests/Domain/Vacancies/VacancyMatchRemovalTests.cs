using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class VacancyMatchRemovalTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static Vacancy Build(VacancyStage stage, Guid hypothesisId, params Guid[] extraHypotheses)
  {
    var vacancy = new Vacancy(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual, Now);
    vacancy.SetMatch(hypothesisId, null, null, MatchAuthor.Human);
    foreach (var extra in extraHypotheses)
    {
      vacancy.SetMatch(extra, null, null, MatchAuthor.Human);
    }

    switch (stage)
    {
      case VacancyStage.Dismissed:
        vacancy.Dismiss(DismissReason.Other, null, Now);
        break;
      case VacancyStage.Targeted:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        break;
      case VacancyStage.Preparing:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        vacancy.MoveTo(VacancyStage.Preparing, Now);
        break;
      case VacancyStage.Applied:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        Apply(vacancy, hypothesisId);
        break;
      case VacancyStage.Interviewing:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        Apply(vacancy, hypothesisId);
        vacancy.MoveTo(VacancyStage.Interviewing, Now);
        break;
      case VacancyStage.Closed:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        Apply(vacancy, hypothesisId);
        vacancy.Close(ApplicationOutcome.Rejected, Now);
        break;
    }

    return vacancy;
  }

  private static void Apply(Vacancy vacancy, Guid hypothesisId)
  {
    vacancy.Apply(hypothesisId, Guid.NewGuid(), ApplicationChannel.Website, Now, null);
  }

  public static IEnumerable<object[]> OffBoardStages() =>
    StageTable.StagesWhere(s => s is VacancyStage.Inbox or VacancyStage.Dismissed);

  public static IEnumerable<object[]> BoardStages() =>
    StageTable.StagesWhere(s => s is not (VacancyStage.Inbox or VacancyStage.Dismissed));

  public static IEnumerable<object[]> AppliedStages() =>
    StageTable.StagesWhere(s => s is VacancyStage.Applied or VacancyStage.Interviewing or VacancyStage.Closed);

  [Theory]
  [MemberData(nameof(OffBoardStages))]
  public void RemoveMatch_LastMatchOffBoard_Allowed(VacancyStage stage)
  {
    var hypothesisId = Guid.NewGuid();
    var vacancy = Build(stage, hypothesisId);

    vacancy.RemoveMatch(hypothesisId);

    Assert.Empty(vacancy.Matches);
  }

  [Theory]
  [MemberData(nameof(BoardStages))]
  public void RemoveMatch_LastMatchOnBoard_ThrowsAndKeepsMatch(VacancyStage stage)
  {
    var hypothesisId = Guid.NewGuid();
    var vacancy = Build(stage, hypothesisId);

    Assert.Throws<InvalidOperationException>(() => vacancy.RemoveMatch(hypothesisId));

    Assert.Single(vacancy.Matches);
  }

  [Theory]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public void RemoveMatch_OneOfTwoOnBoard_Allowed(VacancyStage stage)
  {
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();
    var vacancy = Build(stage, first, second);

    vacancy.RemoveMatch(second);

    Assert.Equal(first, Assert.Single(vacancy.Matches).HypothesisId);
  }

  [Theory]
  [MemberData(nameof(AppliedStages))]
  public void RemoveMatch_OtherThanApplicationHypothesis_Allowed(VacancyStage stage)
  {
    var applied = Guid.NewGuid();
    var other = Guid.NewGuid();
    var vacancy = Build(stage, applied, other);

    vacancy.RemoveMatch(other);

    Assert.Equal(applied, Assert.Single(vacancy.Matches).HypothesisId);
  }

  [Theory]
  [MemberData(nameof(AppliedStages))]
  public void RemoveMatch_ApplicationHypothesisWithOthersPresent_ThrowsAndKeepsMatch(VacancyStage stage)
  {
    var applied = Guid.NewGuid();
    var other = Guid.NewGuid();
    var vacancy = Build(stage, applied, other);

    Assert.Throws<InvalidOperationException>(() => vacancy.RemoveMatch(applied));

    Assert.Equal(2, vacancy.Matches.Count);
  }

  [Fact]
  public void RemoveMatch_NotMatchedHypothesisOnBoard_IsNoOp()
  {
    var hypothesisId = Guid.NewGuid();
    var vacancy = Build(VacancyStage.Targeted, hypothesisId);

    vacancy.RemoveMatch(Guid.NewGuid());

    Assert.Single(vacancy.Matches);
  }
}
