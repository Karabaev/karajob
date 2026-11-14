using Karajob.Domain.Vacancies;

namespace Karajob.Application.Vacancies;

public enum BoardMoveKind
{
  Direct = 0,
  Apply = 1,
  Close = 2,
  Invalid = 3,
}

public static class BoardMoves
{
  public static IReadOnlyList<VacancyStage> Columns { get; } =
  [
    VacancyStage.Targeted,
    VacancyStage.Preparing,
    VacancyStage.Applied,
    VacancyStage.Interviewing,
    VacancyStage.Closed,
  ];

  public static BoardMoveKind Classify(VacancyStage from, VacancyStage to)
  {
    if (from == to || !Columns.Contains(from) || !Columns.Contains(to) || !VacancyStageRules.CanMove(from, to))
    {
      return BoardMoveKind.Invalid;
    }

    if (to == VacancyStage.Closed)
    {
      return BoardMoveKind.Close;
    }

    return to == VacancyStage.Applied && from != VacancyStage.Closed ? BoardMoveKind.Apply : BoardMoveKind.Direct;
  }

  public static VacancyStage? Next(VacancyStage stage)
  {
    return stage switch
    {
      VacancyStage.Targeted => VacancyStage.Preparing,
      VacancyStage.Preparing => VacancyStage.Applied,
      VacancyStage.Applied => VacancyStage.Interviewing,
      VacancyStage.Interviewing => VacancyStage.Closed,
      _ => null,
    };
  }
}
