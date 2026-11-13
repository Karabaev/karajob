namespace Karajob.Domain.Vacancies;

public class StageTransitionException(VacancyStage from, VacancyStage to, string message)
  : InvalidOperationException(message)
{
  public VacancyStage From { get; } = from;
  public VacancyStage To { get; } = to;
}
