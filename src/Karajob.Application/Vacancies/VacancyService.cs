using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Vacancies;

namespace Karajob.Application.Vacancies;

public class VacancyService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, TimeProvider timeProvider)
{
  public Task<IReadOnlyList<VacancyListItem>> ListAsync(VacancyStage stage, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<VacancyDetails?> FindAsync(Guid vacancyId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<VacancyDetails> CreateAsync(VacancyInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<VacancyDetails> UpdateAsync(Guid vacancyId, VacancyInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IReadOnlyList<SimilarVacancy>> FindSimilarAsync(
    Guid companyId,
    Guid roleId,
    Guid? excludeVacancyId,
    CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<TimelineItem> AddEventAsync(Guid vacancyId, VacancyEventInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteEventAsync(Guid vacancyId, Guid eventId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
