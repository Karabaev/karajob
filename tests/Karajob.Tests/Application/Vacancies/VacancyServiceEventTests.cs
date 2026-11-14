using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceEventTests : VacancyServiceFixture
{
  [Fact]
  public async Task FindAsync_WithStageChangesAndManualEvents_TimelineIsNewestFirst()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));
    await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now.AddHours(3), "later"));
    await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now.AddHours(2), "middle"));

    var found = await Service.FindAsync(created.Id);

    Assert.Equal(
      [Now.AddHours(3), Now.AddHours(2), Now.AddHours(1)],
      found!.Timeline.Select(t => t.OccurredAt));
    var stageChanged = found.Timeline[2];
    Assert.Equal(ApplicationEventType.StageChanged, stageChanged.Type);
    Assert.Equal(VacancyStage.Inbox, stageChanged.FromStage);
    Assert.Equal(VacancyStage.Targeted, stageChanged.ToStage);
  }

  [Fact]
  public async Task AddEventAsync_NoteOnInboxVacancy_ReturnsItemAndAppearsInTimeline()
  {
    var created = await Service.CreateAsync(Input());
    var at = Now.AddHours(1);

    var item = await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, at, "called HR"));

    Assert.NotEqual(Guid.Empty, item.Id);
    Assert.Equal(ApplicationEventType.Note, item.Type);
    Assert.Equal(at, item.OccurredAt);
    Assert.Equal("called HR", item.Note);
    Assert.Null(item.FromStage);
    Assert.Null(item.ToStage);
    var found = await Service.FindAsync(created.Id);
    Assert.Equal(item, Assert.Single(found!.Timeline));
  }

  [Fact]
  public async Task AddEventAsync_BlankNote_StoredAsNull()
  {
    var created = await Service.CreateAsync(Input());

    var item = await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now, "   "));

    Assert.Null(item.Note);
    var found = await Service.FindAsync(created.Id);
    Assert.Null(Assert.Single(found!.Timeline).Note);
  }

  [Fact]
  public async Task AddEventAsync_ScreeningOnAppliedVacancy_Succeeds()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);
    await MutateAsync(created.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(hypothesisId, cvVersion.Id, ApplicationChannel.Website, Now.AddHours(1), null);
    });

    var item = await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Screening, Now.AddDays(1), null));

    Assert.Equal(ApplicationEventType.Screening, item.Type);
    var found = await Service.FindAsync(created.Id);
    Assert.Equal(VacancyStage.Applied, found!.Stage);
  }

  [Theory]
  [InlineData(ApplicationEventType.Screening)]
  [InlineData(ApplicationEventType.TechInterview)]
  [InlineData(ApplicationEventType.TestTask)]
  public async Task AddEventAsync_InterviewTypeWithoutApplication_FailsWithTypeError(ApplicationEventType type)
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.AddEventAsync(created.Id, new VacancyEventInput(type, Now, null)));

    Assert.Contains("Type", ex.Errors.Keys);
    Assert.Empty((await Service.FindAsync(created.Id))!.Timeline);
  }

  [Theory]
  [InlineData(ApplicationEventType.StageChanged)]
  [InlineData(ApplicationEventType.Offer)]
  [InlineData(ApplicationEventType.Rejected)]
  public async Task AddEventAsync_ReservedType_FailsWithTypeError(ApplicationEventType type)
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.AddEventAsync(created.Id, new VacancyEventInput(type, Now, null)));

    Assert.Contains("Type", ex.Errors.Keys);
  }

  [Fact]
  public async Task AddEventAsync_NonUtcOccurredAt_ThrowsArgumentExceptionNotValidation()
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Record.ExceptionAsync(() =>
      Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), null)));

    Assert.IsType<ArgumentException>(ex);
  }

  [Fact]
  public async Task AddEventAsync_BeforeDiscoveryDay_FailsWithOccurredAtError()
  {
    var created = await Service.CreateAsync(Input());

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now.Date.AddSeconds(-1), null)));

    Assert.Contains("OccurredAt", ex.Errors.Keys);
    Assert.Empty((await Service.FindAsync(created.Id))!.Timeline);
  }

  [Fact]
  public async Task AddEventAsync_StartOfDiscoveryDay_Succeeds()
  {
    var created = await Service.CreateAsync(Input());

    var item = await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now.Date, null));

    Assert.Equal(Now.Date, item.OccurredAt);
  }

  [Fact]
  public async Task AddEventAsync_UnknownVacancy_Throws()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
      Service.AddEventAsync(Guid.NewGuid(), new VacancyEventInput(ApplicationEventType.Note, Now, null)));
  }

  [Fact]
  public async Task AddEventAsync_AnotherUsersVacancy_Throws()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
      Service.AddEventAsync(foreign.Id, new VacancyEventInput(ApplicationEventType.Note, Now, null)));
  }

  [Fact]
  public async Task DeleteEventAsync_ManualEvent_RemovesItFromTimeline()
  {
    var created = await Service.CreateAsync(Input());
    var item = await Service.AddEventAsync(created.Id, new VacancyEventInput(ApplicationEventType.Note, Now, "x"));

    await Service.DeleteEventAsync(created.Id, item.Id);

    Assert.Empty((await Service.FindAsync(created.Id))!.Timeline);
  }

  [Fact]
  public async Task DeleteEventAsync_StageChangedEvent_FailsWithGeneralErrorAndKeepsEvent()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now));
    var stageChanged = Assert.Single((await Service.FindAsync(created.Id))!.Timeline);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteEventAsync(created.Id, stageChanged.Id));

    Assert.Contains("", ex.Errors.Keys);
    Assert.Single((await Service.FindAsync(created.Id))!.Timeline);
  }

  [Fact]
  public async Task DeleteEventAsync_UnknownEvent_Throws()
  {
    var created = await Service.CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteEventAsync(created.Id, Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteEventAsync_EventOfAnotherVacancy_ThrowsAndKeepsEvent()
  {
    var first = await Service.CreateAsync(Input("https://example.com/jobs/1"));
    var second = await Service.CreateAsync(Input("https://example.com/jobs/2"));
    var item = await Service.AddEventAsync(first.Id, new VacancyEventInput(ApplicationEventType.Note, Now, "x"));

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteEventAsync(second.Id, item.Id));

    Assert.Single((await Service.FindAsync(first.Id))!.Timeline);
  }

  [Fact]
  public async Task DeleteEventAsync_UnknownVacancy_Throws()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteEventAsync(Guid.NewGuid(), Guid.NewGuid()));
  }
}
