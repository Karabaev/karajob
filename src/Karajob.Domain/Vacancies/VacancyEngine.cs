using Karajob.Domain.Common;

namespace Karajob.Domain.Vacancies;

public class VacancyEngine : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid VacancyId { get; private set; }
  public Guid EngineId { get; private set; }

  internal VacancyEngine(Guid userId, Guid vacancyId, Guid engineId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    VacancyId = Require.NotEmpty(vacancyId);
    EngineId = Require.NotEmpty(engineId);
  }
}
