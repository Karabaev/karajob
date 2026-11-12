using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Applications;

public class ApplicationEventTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Ctor_StageChanged_SetsAllFields()
  {
    var userId = Guid.NewGuid();
    var vacancyId = Guid.NewGuid();

    var evt = new ApplicationEvent(
      userId, vacancyId, ApplicationEventType.StageChanged, Now, VacancyStage.Inbox, VacancyStage.Targeted, "moved");

    Assert.NotEqual(Guid.Empty, evt.Id);
    Assert.Equal(userId, evt.UserId);
    Assert.Equal(vacancyId, evt.VacancyId);
    Assert.Equal(ApplicationEventType.StageChanged, evt.Type);
    Assert.Equal(Now, evt.OccurredAt);
    Assert.Equal(VacancyStage.Inbox, evt.FromStage);
    Assert.Equal(VacancyStage.Targeted, evt.ToStage);
    Assert.Equal("moved", evt.Note);
  }

  [Fact]
  public void Ctor_StageChangedWithoutToStage_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new ApplicationEvent(
        Guid.NewGuid(), Guid.NewGuid(), ApplicationEventType.StageChanged, Now, VacancyStage.Inbox, null, null));
  }

  [Fact]
  public void Ctor_NoteWithoutStages_Succeeds()
  {
    var evt = new ApplicationEvent(
      Guid.NewGuid(), Guid.NewGuid(), ApplicationEventType.Note, Now, null, null, "call scheduled");

    Assert.Null(evt.FromStage);
    Assert.Null(evt.ToStage);
    Assert.Equal("call scheduled", evt.Note);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new ApplicationEvent(Guid.Empty, Guid.NewGuid(), ApplicationEventType.Note, Now, null, null, null));
  }

  [Fact]
  public void Ctor_EmptyVacancyId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new ApplicationEvent(Guid.NewGuid(), Guid.Empty, ApplicationEventType.Note, Now, null, null, null));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcOccurredAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new ApplicationEvent(
        Guid.NewGuid(), Guid.NewGuid(), ApplicationEventType.Note, new DateTime(2026, 11, 12, 10, 0, 0, kind),
        null, null, null));
  }
}
