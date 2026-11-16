using Karajob.Domain.Common;

namespace Karajob.Domain.Vacancies;

public class VacancyPlatform : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid VacancyId { get; private set; }
  public Guid PlatformId { get; private set; }

  internal VacancyPlatform(Guid userId, Guid vacancyId, Guid platformId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    VacancyId = Require.NotEmpty(vacancyId);
    PlatformId = Require.NotEmpty(platformId);
  }
}
