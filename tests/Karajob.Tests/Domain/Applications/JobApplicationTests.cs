using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Applications;

public class JobApplicationTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static (Vacancy Vacancy, Guid HypothesisId) CreateTargeted()
  {
    var vacancy = new Vacancy(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual, Now);
    var hypothesisId = Guid.NewGuid();
    vacancy.SetMatch(hypothesisId, null, null, MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Now);
    return (vacancy, hypothesisId);
  }

  [Fact]
  public void Apply_Valid_SetsAllFields()
  {
    var (vacancy, hypothesisId) = CreateTargeted();
    var cvVersionId = Guid.NewGuid();

    vacancy.Apply(hypothesisId, cvVersionId, ApplicationChannel.Referral, Now, "Hello");

    var application = vacancy.Application;
    Assert.NotNull(application);
    Assert.NotEqual(Guid.Empty, application.Id);
    Assert.Equal(vacancy.UserId, application.UserId);
    Assert.Equal(vacancy.Id, application.VacancyId);
    Assert.Equal(hypothesisId, application.HypothesisId);
    Assert.Equal(cvVersionId, application.CvVersionId);
    Assert.Equal(ApplicationChannel.Referral, application.Channel);
    Assert.Equal(Now, application.AppliedAt);
    Assert.Equal("Hello", application.CoverLetter);
    Assert.Null(application.Outcome);
  }

  [Fact]
  public void Apply_EmptyHypothesisId_Throws()
  {
    var (vacancy, _) = CreateTargeted();

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(Guid.Empty, Guid.NewGuid(), ApplicationChannel.Website, Now, null));
  }

  [Fact]
  public void Apply_EmptyCvVersionId_Throws()
  {
    var (vacancy, hypothesisId) = CreateTargeted();

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(hypothesisId, Guid.Empty, ApplicationChannel.Website, Now, null));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Apply_NonUtcAppliedAt_Throws(DateTimeKind kind)
  {
    var (vacancy, hypothesisId) = CreateTargeted();

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(
        hypothesisId, Guid.NewGuid(), ApplicationChannel.Website,
        new DateTime(2026, 11, 12, 10, 0, 0, kind), null));
  }
}
