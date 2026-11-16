using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceCoverLetterTests : VacancyServiceFixture
{
  private const string MultiLineLetter = "Hello,\n\nI would like to join your team.\nBest regards";

  [Fact]
  public async Task FindAsync_AppliedWithCoverLetter_ReturnsLetterAndHasApplication()
  {
    var id = await CreateAppliedAsync(MultiLineLetter);

    var found = await Service.FindAsync(id);

    Assert.Equal(VacancyStage.Applied, found!.Stage);
    Assert.Equal(MultiLineLetter, found.CoverLetter);
    Assert.True(found.HasApplication);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task FindAsync_AppliedWithBlankCoverLetter_ReturnsNullLetterAndHasApplication(string? letter)
  {
    var id = await CreateAppliedAsync(letter);

    var found = await Service.FindAsync(id);

    Assert.Null(found!.CoverLetter);
    Assert.True(found.HasApplication);
  }

  [Fact]
  public async Task FindAsync_InterviewingWithCoverLetter_ReturnsLetter()
  {
    var id = await CreateAppliedAsync(MultiLineLetter);
    await MutateAsync(id, v => v.MoveTo(VacancyStage.Interviewing, Now));

    var found = await Service.FindAsync(id);

    Assert.Equal(VacancyStage.Interviewing, found!.Stage);
    Assert.Equal(MultiLineLetter, found.CoverLetter);
  }

  [Theory]
  [InlineData(ApplicationOutcome.Offer)]
  [InlineData(ApplicationOutcome.Rejected)]
  [InlineData(ApplicationOutcome.Ghosted)]
  [InlineData(ApplicationOutcome.Withdrawn)]
  public async Task FindAsync_ClosedAfterApplyingWithCoverLetter_ReturnsLetter(ApplicationOutcome outcome)
  {
    var id = await CreateAppliedAsync(MultiLineLetter);
    await MutateAsync(id, v => v.Close(outcome, Now));

    var found = await Service.FindAsync(id);

    Assert.Equal(VacancyStage.Closed, found!.Stage);
    Assert.Equal(MultiLineLetter, found.CoverLetter);
    Assert.True(found.HasApplication);
  }

  [Fact]
  public async Task FindAsync_TargetedVacancy_HasNoApplicationAndNoLetter()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId);

    var found = await Service.FindAsync(id);

    Assert.False(found!.HasApplication);
    Assert.Null(found.CoverLetter);
  }

  private async Task<Guid> CreateAppliedAsync(string? coverLetter)
  {
    var hypothesisId = await CreateHypothesisAsync();
    var (_, cvVersionId) = await CreateCvAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId);
    await Service.ApplyAsync(
      id,
      VacancyStage.Targeted,
      new ApplyInput(hypothesisId, cvVersionId, ApplicationChannel.Website, Now, coverLetter));
    return id;
  }
}
