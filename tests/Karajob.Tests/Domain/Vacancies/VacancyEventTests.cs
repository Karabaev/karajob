using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class VacancyEventTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static Vacancy Inbox() => new(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual, Now);

  private static Vacancy Targeted()
  {
    var vacancy = Inbox();
    vacancy.SetMatch(Guid.NewGuid(), null, null, MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Now);
    return vacancy;
  }

  private static Vacancy Applied()
  {
    var vacancy = Inbox();
    var hypothesisId = Guid.NewGuid();
    vacancy.SetMatch(hypothesisId, null, null, MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Now);
    vacancy.Apply(hypothesisId, Guid.NewGuid(), ApplicationChannel.Website, Now, null);
    return vacancy;
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  [InlineData(ApplicationEventType.Note)]
  public void AddEvent_ManualTypeOnAppliedVacancy_AddsEventWithoutStages(ApplicationEventType type)
  {
    var vacancy = Applied();
    var at = Now.AddDays(1);

    var added = vacancy.AddEvent(type, at, "text");

    Assert.Contains(added, vacancy.Events);
    Assert.Equal(type, added.Type);
    Assert.Equal(at, added.OccurredAt);
    Assert.Equal("text", added.Note);
    Assert.Null(added.FromStage);
    Assert.Null(added.ToStage);
    Assert.Equal(vacancy.Id, added.VacancyId);
    Assert.Equal(vacancy.UserId, added.UserId);
  }

  [Fact]
  public void AddEvent_BeforeDiscoveryDay_ThrowsForAt()
  {
    var vacancy = Inbox();

    var ex = Assert.Throws<ArgumentException>(() => vacancy.AddEvent(ApplicationEventType.Note, Now.Date.AddTicks(-1), null));

    Assert.Equal("at", ex.ParamName);
  }

  [Fact]
  public void AddEvent_StartOfDiscoveryDay_IsAllowed()
  {
    var vacancy = Inbox();

    var added = vacancy.AddEvent(ApplicationEventType.Note, Now.Date, null);

    Assert.Equal(Now.Date, added.OccurredAt);
  }

  [Fact]
  public void AddEvent_Screening_DoesNotChangeStage()
  {
    var vacancy = Applied();

    vacancy.AddEvent(ApplicationEventType.Screening, Now.AddDays(1), null);

    Assert.Equal(VacancyStage.Applied, vacancy.Stage);
  }

  [Theory]
  [InlineData(ApplicationEventType.StageChanged)]
  [InlineData(ApplicationEventType.Rejected)]
  [InlineData(ApplicationEventType.Offer)]
  public void AddEvent_ReservedType_Throws(ApplicationEventType type)
  {
    var vacancy = Applied();

    Assert.Throws<ArgumentException>(() => vacancy.AddEvent(type, Now, null));
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  public void AddEvent_InterviewTypeInInbox_Throws(ApplicationEventType type)
  {
    Assert.Throws<InvalidOperationException>(() => Inbox().AddEvent(type, Now, null));
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  public void AddEvent_InterviewTypeInTargeted_Throws(ApplicationEventType type)
  {
    Assert.Throws<InvalidOperationException>(() => Targeted().AddEvent(type, Now, null));
  }

  [Fact]
  public void AddEvent_NoteInInbox_Allowed()
  {
    var vacancy = Inbox();

    var added = vacancy.AddEvent(ApplicationEventType.Note, Now, "remember");

    Assert.Contains(added, vacancy.Events);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void AddEvent_NonUtc_Throws(DateTimeKind kind)
  {
    var vacancy = Inbox();

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.AddEvent(ApplicationEventType.Note, new DateTime(2026, 11, 12, 10, 0, 0, kind), null));
  }

  [Fact]
  public void RemoveEvent_ManualEvent_RemovesIt()
  {
    var vacancy = Inbox();
    var added = vacancy.AddEvent(ApplicationEventType.Note, Now, null);

    vacancy.RemoveEvent(added.Id);

    Assert.DoesNotContain(added, vacancy.Events);
  }

  [Fact]
  public void RemoveEvent_StageChangedEvent_ThrowsAndKeepsEvent()
  {
    var vacancy = Targeted();
    var stageChanged = vacancy.Events.Single();

    Assert.Throws<InvalidOperationException>(() => vacancy.RemoveEvent(stageChanged.Id));

    Assert.Contains(stageChanged, vacancy.Events);
  }

  [Fact]
  public void RemoveEvent_UnknownId_Throws()
  {
    Assert.Throws<KeyNotFoundException>(() => Inbox().RemoveEvent(Guid.NewGuid()));
  }
}
