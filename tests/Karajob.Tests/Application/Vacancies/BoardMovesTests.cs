using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Application.Vacancies;

public class BoardMovesTests
{
  [Fact]
  public void Columns_AreBoardStagesInOrder()
  {
    Assert.Equal(
      [VacancyStage.Targeted, VacancyStage.Preparing, VacancyStage.Applied, VacancyStage.Interviewing, VacancyStage.Closed],
      BoardMoves.Columns);
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Preparing)]
  [InlineData(VacancyStage.Preparing, VacancyStage.Targeted)]
  [InlineData(VacancyStage.Applied, VacancyStage.Interviewing)]
  [InlineData(VacancyStage.Closed, VacancyStage.Applied)]
  [InlineData(VacancyStage.Closed, VacancyStage.Interviewing)]
  public void Classify_AllowedMoveWithoutDialog_IsDirect(VacancyStage from, VacancyStage to)
  {
    Assert.Equal(BoardMoveKind.Direct, BoardMoves.Classify(from, to));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public void Classify_ToAppliedFromPreApplication_IsApply(VacancyStage from)
  {
    Assert.Equal(BoardMoveKind.Apply, BoardMoves.Classify(from, VacancyStage.Applied));
  }

  [Theory]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public void Classify_ToClosedFromApplication_IsClose(VacancyStage from)
  {
    Assert.Equal(BoardMoveKind.Close, BoardMoves.Classify(from, VacancyStage.Closed));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Interviewing)]
  [InlineData(VacancyStage.Targeted, VacancyStage.Closed)]
  [InlineData(VacancyStage.Preparing, VacancyStage.Closed)]
  [InlineData(VacancyStage.Applied, VacancyStage.Targeted)]
  [InlineData(VacancyStage.Interviewing, VacancyStage.Applied)]
  public void Classify_TransitionNotAllowed_IsInvalid(VacancyStage from, VacancyStage to)
  {
    Assert.Equal(BoardMoveKind.Invalid, BoardMoves.Classify(from, to));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Targeted)]
  [InlineData(VacancyStage.Closed, VacancyStage.Closed)]
  public void Classify_SameStage_IsInvalid(VacancyStage from, VacancyStage to)
  {
    Assert.Equal(BoardMoveKind.Invalid, BoardMoves.Classify(from, to));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Inbox)]
  [InlineData(VacancyStage.Targeted, VacancyStage.Dismissed)]
  [InlineData(VacancyStage.Preparing, VacancyStage.Dismissed)]
  [InlineData(VacancyStage.Inbox, VacancyStage.Targeted)]
  [InlineData(VacancyStage.Dismissed, VacancyStage.Preparing)]
  public void Classify_TargetOrSourceOffBoard_IsInvalid(VacancyStage from, VacancyStage to)
  {
    Assert.Equal(BoardMoveKind.Invalid, BoardMoves.Classify(from, to));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted, VacancyStage.Preparing)]
  [InlineData(VacancyStage.Preparing, VacancyStage.Applied)]
  [InlineData(VacancyStage.Applied, VacancyStage.Interviewing)]
  [InlineData(VacancyStage.Interviewing, VacancyStage.Closed)]
  public void Next_BoardStage_IsFollowingColumn(VacancyStage stage, VacancyStage expected)
  {
    Assert.Equal(expected, BoardMoves.Next(stage));
  }

  [Theory]
  [InlineData(VacancyStage.Closed)]
  [InlineData(VacancyStage.Inbox)]
  [InlineData(VacancyStage.Dismissed)]
  public void Next_StageWithoutSuccessor_IsNull(VacancyStage stage)
  {
    Assert.Null(BoardMoves.Next(stage));
  }
}
