using Karajob.Domain.Common;
using Karajob.Domain.Vacancies;

namespace Karajob.Domain.Applications;

public class ApplicationEvent : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid VacancyId { get; private set; }
  public ApplicationEventType Type { get; private set; }
  public DateTime OccurredAt { get; private set; }
  public VacancyStage? FromStage { get; private set; }
  public VacancyStage? ToStage { get; private set; }
  public string? Note { get; set; }

  public ApplicationEvent(
    Guid userId,
    Guid vacancyId,
    ApplicationEventType type,
    DateTime occurredAt,
    VacancyStage? fromStage,
    VacancyStage? toStage,
    string? note)
  {
    if (type == ApplicationEventType.StageChanged && toStage is null) throw new ArgumentException("StageChanged requires a target stage.", nameof(toStage));
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    VacancyId = Require.NotEmpty(vacancyId);
    Type = type;
    OccurredAt = UtcGuard.Ensure(occurredAt);
    FromStage = fromStage;
    ToStage = toStage;
    Note = note;
  }
}
