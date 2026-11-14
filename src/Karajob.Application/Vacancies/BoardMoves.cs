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
    throw new NotImplementedException();
  }

  public static VacancyStage? Next(VacancyStage stage)
  {
    throw new NotImplementedException();
  }
}
