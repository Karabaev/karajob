using Karajob.Domain.Common;

namespace Karajob.Domain.Applications;

public class JobApplication : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid VacancyId { get; private set; }
  public Guid TrackId { get; private set; }
  public Guid CvVersionId { get; private set; }
  public ApplicationChannel Channel { get; private set; }
  public DateTime AppliedAt { get; private set; }
  public string? CoverLetter { get; set; }
  public ApplicationOutcome? Outcome { get; private set; }

  internal void SetOutcome(ApplicationOutcome? outcome)
  {
    Outcome = outcome;
  }

  internal JobApplication(
    Guid userId,
    Guid vacancyId,
    Guid trackId,
    Guid cvVersionId,
    ApplicationChannel channel,
    DateTime appliedAt,
    string? coverLetter)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    VacancyId = Require.NotEmpty(vacancyId);
    TrackId = Require.NotEmpty(trackId);
    CvVersionId = Require.NotEmpty(cvVersionId);
    Channel = channel;
    AppliedAt = UtcGuard.Ensure(appliedAt);
    CoverLetter = coverLetter;
  }
}
