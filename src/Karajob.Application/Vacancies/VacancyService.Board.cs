using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Application.Vacancies;

public partial class VacancyService
{
  public Task<BoardView> GetBoardAsync(Guid? hypothesisId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task MoveAsync(Guid vacancyId, VacancyStage expectedStage, VacancyStage target, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<ApplyOptions> GetApplyOptionsAsync(Guid vacancyId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task ApplyAsync(Guid vacancyId, VacancyStage expectedStage, ApplyInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task CloseAsync(Guid vacancyId, VacancyStage expectedStage, ApplicationOutcome outcome, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
