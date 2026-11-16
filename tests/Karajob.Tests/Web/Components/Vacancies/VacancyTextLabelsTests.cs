using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Web.Components.Vacancies;

namespace Karajob.Tests.Web.Components.Vacancies;

public class VacancyTextLabelsTests
{
  [Theory]
  [InlineData(VacancyStage.Inbox, "Inbox")]
  [InlineData(VacancyStage.Targeted, "Targeted")]
  [InlineData(VacancyStage.Preparing, "Preparing")]
  [InlineData(VacancyStage.Applied, "Applied")]
  [InlineData(VacancyStage.Interviewing, "Interviewing")]
  [InlineData(VacancyStage.Closed, "Closed")]
  [InlineData(VacancyStage.Dismissed, "Dismissed")]
  public void Stage_ReturnsEnglishLabel(VacancyStage stage, string expected)
  {
    Assert.Equal(expected, VacancyText.Stage(stage));
  }

  [Theory]
  [InlineData(ApplicationEventType.StageChanged, "Stage changed")]
  [InlineData(ApplicationEventType.Screening, "Screening")]
  [InlineData(ApplicationEventType.TechInterview, "Technical interview")]
  [InlineData(ApplicationEventType.TestTask, "Test task")]
  [InlineData(ApplicationEventType.Rejected, "Rejected")]
  [InlineData(ApplicationEventType.Offer, "Offer")]
  [InlineData(ApplicationEventType.Note, "Note")]
  public void EventType_ReturnsEnglishLabel(ApplicationEventType type, string expected)
  {
    Assert.Equal(expected, VacancyText.EventType(type));
  }

  [Theory]
  [InlineData(DismissReason.WrongRole, "Wrong role")]
  [InlineData(DismissReason.WrongLocation, "Wrong location")]
  [InlineData(DismissReason.WrongStack, "Wrong stack")]
  [InlineData(DismissReason.WrongSalary, "Wrong salary")]
  [InlineData(DismissReason.Other, "Other")]
  public void Dismiss_ReturnsEnglishLabel(DismissReason reason, string expected)
  {
    Assert.Equal(expected, VacancyText.Dismiss(reason));
  }

  [Theory]
  [InlineData(ApplicationOutcome.Offer, "Offer")]
  [InlineData(ApplicationOutcome.Rejected, "Rejected")]
  [InlineData(ApplicationOutcome.Ghosted, "Ghosted")]
  [InlineData(ApplicationOutcome.Withdrawn, "Withdrawn")]
  public void Outcome_ReturnsEnglishLabel(ApplicationOutcome outcome, string expected)
  {
    Assert.Equal(expected, VacancyText.Outcome(outcome));
  }

  [Theory]
  [InlineData(ApplicationChannel.Website, "Website")]
  [InlineData(ApplicationChannel.Referral, "Referral")]
  [InlineData(ApplicationChannel.Recruiter, "Recruiter")]
  [InlineData(ApplicationChannel.ColdEmail, "Cold email")]
  [InlineData(ApplicationChannel.Other, "Other")]
  public void Channel_ReturnsEnglishLabel(ApplicationChannel channel, string expected)
  {
    Assert.Equal(expected, VacancyText.Channel(channel));
  }
}
