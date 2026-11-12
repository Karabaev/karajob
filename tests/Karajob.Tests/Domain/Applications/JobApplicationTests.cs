using Karajob.Domain.Applications;

namespace Karajob.Tests.Domain.Applications;

public class JobApplicationTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Ctor_Valid_SetsAllFields()
  {
    var userId = Guid.NewGuid();
    var vacancyId = Guid.NewGuid();
    var hypothesisId = Guid.NewGuid();
    var cvVersionId = Guid.NewGuid();

    var application = new JobApplication(
      userId, vacancyId, hypothesisId, cvVersionId, ApplicationChannel.Referral, Now, "Hello");

    Assert.NotEqual(Guid.Empty, application.Id);
    Assert.Equal(userId, application.UserId);
    Assert.Equal(vacancyId, application.VacancyId);
    Assert.Equal(hypothesisId, application.HypothesisId);
    Assert.Equal(cvVersionId, application.CvVersionId);
    Assert.Equal(ApplicationChannel.Referral, application.Channel);
    Assert.Equal(Now, application.AppliedAt);
    Assert.Equal("Hello", application.CoverLetter);
    Assert.Null(application.Outcome);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public void Ctor_EmptyId_Throws(int emptyIndex)
  {
    var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
    ids[emptyIndex] = Guid.Empty;

    Assert.ThrowsAny<ArgumentException>(() =>
      new JobApplication(ids[0], ids[1], ids[2], ids[3], ApplicationChannel.Website, Now, null));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcAppliedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new JobApplication(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ApplicationChannel.Website,
        new DateTime(2026, 11, 12, 10, 0, 0, kind), null));
  }
}
