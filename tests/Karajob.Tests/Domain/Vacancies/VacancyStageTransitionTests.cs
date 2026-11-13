using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class VacancyStageTransitionTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
  private static readonly DateTime Later = Now.AddDays(1);
  private static readonly Guid CvId = Guid.NewGuid();

  public static IEnumerable<object[]> AllowedPairs() => StageTable.AllowedPairs();

  public static IEnumerable<object[]> MoveToForbiddenTargets() =>
    StageTable.AllPairs().Where(p =>
      (VacancyStage)p[1] is VacancyStage.Applied or VacancyStage.Closed or VacancyStage.Dismissed);

  public static IEnumerable<object[]> MoveToDisallowedPairs() =>
    StageTable.DisallowedPairs().Where(p =>
      (VacancyStage)p[1] is not (VacancyStage.Applied or VacancyStage.Closed or VacancyStage.Dismissed));

  public static IEnumerable<object[]> DismissDisallowedStages() =>
    StageTable.StagesWhere(s => s is not (VacancyStage.Inbox or VacancyStage.Targeted or VacancyStage.Preparing));

  public static IEnumerable<object[]> ApplyDisallowedStages() =>
    StageTable.StagesWhere(s => s is not (VacancyStage.Targeted or VacancyStage.Preparing));

  public static IEnumerable<object[]> CloseDisallowedStages() =>
    StageTable.StagesWhere(s => s is not (VacancyStage.Applied or VacancyStage.Interviewing));

  public static IEnumerable<object[]> NonUtcKinds() =>
  [
    [DateTimeKind.Local],
    [DateTimeKind.Unspecified],
  ];

  private static Vacancy Create() => new(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual, Now);

  private static Guid AddMatch(Vacancy vacancy)
  {
    var hypothesisId = Guid.NewGuid();
    vacancy.SetMatch(hypothesisId, 80, null, MatchAuthor.Human);
    return hypothesisId;
  }

  private static Vacancy InStage(VacancyStage stage, bool withMatch = true)
  {
    var vacancy = Create();
    var hypothesisId = withMatch ? AddMatch(vacancy) : Guid.Empty;
    switch (stage)
    {
      case VacancyStage.Inbox:
        break;
      case VacancyStage.Targeted:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        break;
      case VacancyStage.Preparing:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        vacancy.MoveTo(VacancyStage.Preparing, Now);
        break;
      case VacancyStage.Applied:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        vacancy.Apply(hypothesisId, CvId, ApplicationChannel.Website, Now, null);
        break;
      case VacancyStage.Interviewing:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        vacancy.Apply(hypothesisId, CvId, ApplicationChannel.Website, Now, null);
        vacancy.MoveTo(VacancyStage.Interviewing, Now);
        break;
      case VacancyStage.Closed:
        vacancy.MoveTo(VacancyStage.Targeted, Now);
        vacancy.Apply(hypothesisId, CvId, ApplicationChannel.Website, Now, null);
        vacancy.Close(ApplicationOutcome.Rejected, Now);
        break;
      case VacancyStage.Dismissed:
        vacancy.Dismiss(DismissReason.Other, null, Now);
        break;
    }

    Assert.Equal(stage, vacancy.Stage);
    return vacancy;
  }

  private static Guid HypothesisOf(Vacancy vacancy) => vacancy.Matches.First().HypothesisId;

  private static ApplicationEvent Perform(Vacancy vacancy, VacancyStage target)
  {
    return target switch
    {
      VacancyStage.Applied when vacancy.Stage is VacancyStage.Targeted or VacancyStage.Preparing =>
        vacancy.Apply(HypothesisOf(vacancy), CvId, ApplicationChannel.Website, Later, null),
      VacancyStage.Closed => vacancy.Close(ApplicationOutcome.Offer, Later),
      VacancyStage.Dismissed => vacancy.Dismiss(DismissReason.WrongRole, null, Later),
      _ => vacancy.MoveTo(target, Later),
    };
  }

  private static void AssertThrowsTransition(Vacancy vacancy, VacancyStage to, Action action)
  {
    var stage = vacancy.Stage;
    var reason = vacancy.DismissReason;
    var count = vacancy.Events.Count;
    var application = vacancy.Application;
    var outcome = application?.Outcome;

    var ex = Assert.Throws<StageTransitionException>(action);

    Assert.Equal(stage, ex.From);
    Assert.Equal(to, ex.To);
    Assert.Equal(stage, vacancy.Stage);
    Assert.Equal(reason, vacancy.DismissReason);
    Assert.Equal(count, vacancy.Events.Count);
    Assert.Same(application, vacancy.Application);
    Assert.Equal(outcome, vacancy.Application?.Outcome);
  }

  [Theory]
  [MemberData(nameof(AllowedPairs))]
  public void Transition_Allowed_ChangesStageAndAppendsEvent(VacancyStage from, VacancyStage to)
  {
    var vacancy = InStage(from);
    var before = vacancy.Events.Count;

    var evt = Perform(vacancy, to);

    Assert.Equal(to, vacancy.Stage);
    Assert.Equal(before + 1, vacancy.Events.Count);
    Assert.Same(evt, vacancy.Events.Last());
    Assert.Equal(ApplicationEventType.StageChanged, evt.Type);
    Assert.Equal(from, evt.FromStage);
    Assert.Equal(to, evt.ToStage);
    Assert.Equal(vacancy.Id, evt.VacancyId);
    Assert.Equal(vacancy.UserId, evt.UserId);
    Assert.Equal(Later, evt.OccurredAt);
  }

  [Theory]
  [MemberData(nameof(MoveToDisallowedPairs))]
  public void MoveTo_DisallowedTransition_ThrowsAndChangesNothing(VacancyStage from, VacancyStage to)
  {
    var vacancy = InStage(from);

    AssertThrowsTransition(vacancy, to, () => vacancy.MoveTo(to, Later));
  }

  [Theory]
  [MemberData(nameof(MoveToForbiddenTargets))]
  public void MoveTo_TargetRequiringDedicatedMethod_Throws(VacancyStage from, VacancyStage to)
  {
    var vacancy = InStage(from);

    AssertThrowsTransition(vacancy, to, () => vacancy.MoveTo(to, Later));
  }

  [Theory]
  [MemberData(nameof(DismissDisallowedStages))]
  public void Dismiss_DisallowedStage_ThrowsAndChangesNothing(VacancyStage from)
  {
    var vacancy = InStage(from);

    AssertThrowsTransition(vacancy, VacancyStage.Dismissed, () => vacancy.Dismiss(DismissReason.Other, "x", Later));
  }

  [Theory]
  [MemberData(nameof(ApplyDisallowedStages))]
  public void Apply_DisallowedStage_ThrowsAndChangesNothing(VacancyStage from)
  {
    var vacancy = InStage(from);

    AssertThrowsTransition(vacancy, VacancyStage.Applied, () =>
      vacancy.Apply(HypothesisOf(vacancy), CvId, ApplicationChannel.Website, Later, null));
  }

  [Theory]
  [MemberData(nameof(CloseDisallowedStages))]
  public void Close_DisallowedStage_ThrowsAndChangesNothing(VacancyStage from)
  {
    var vacancy = InStage(from);

    AssertThrowsTransition(vacancy, VacancyStage.Closed, () => vacancy.Close(ApplicationOutcome.Offer, Later));
  }

  [Fact]
  public void MoveTo_TargetedFromInboxWithoutMatches_Throws()
  {
    var vacancy = InStage(VacancyStage.Inbox, withMatch: false);

    AssertThrowsTransition(vacancy, VacancyStage.Targeted, () => vacancy.MoveTo(VacancyStage.Targeted, Later));
  }

  [Theory]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public void MoveTo_FromDismissedWithoutMatches_Throws(VacancyStage target)
  {
    var vacancy = InStage(VacancyStage.Dismissed, withMatch: false);

    AssertThrowsTransition(vacancy, target, () => vacancy.MoveTo(target, Later));
  }

  [Fact]
  public void MoveTo_InboxFromDismissedWithoutMatches_Succeeds()
  {
    var vacancy = InStage(VacancyStage.Dismissed, withMatch: false);

    vacancy.MoveTo(VacancyStage.Inbox, Later);

    Assert.Equal(VacancyStage.Inbox, vacancy.Stage);
  }

  [Fact]
  public void MoveTo_TargetedAfterAllMatchesRemoved_Throws()
  {
    var vacancy = Create();
    var hypothesisId = AddMatch(vacancy);
    vacancy.RemoveMatch(hypothesisId);

    AssertThrowsTransition(vacancy, VacancyStage.Targeted, () => vacancy.MoveTo(VacancyStage.Targeted, Later));
  }

  [Fact]
  public void Apply_HypothesisNotAmongMatches_ThrowsAndChangesNothing()
  {
    var vacancy = InStage(VacancyStage.Targeted);

    AssertThrowsTransition(vacancy, VacancyStage.Applied, () =>
      vacancy.Apply(Guid.NewGuid(), CvId, ApplicationChannel.Website, Later, null));
    Assert.Null(vacancy.Application);
  }

  [Fact]
  public void Apply_Valid_CreatesApplicationWithAllFields()
  {
    var vacancy = InStage(VacancyStage.Preparing);
    var hypothesisId = HypothesisOf(vacancy);
    var cvVersionId = Guid.NewGuid();

    var evt = vacancy.Apply(hypothesisId, cvVersionId, ApplicationChannel.Referral, Later, "Hello");

    var application = vacancy.Application;
    Assert.NotNull(application);
    Assert.NotEqual(Guid.Empty, application.Id);
    Assert.Equal(vacancy.UserId, application.UserId);
    Assert.Equal(vacancy.Id, application.VacancyId);
    Assert.Equal(hypothesisId, application.HypothesisId);
    Assert.Equal(cvVersionId, application.CvVersionId);
    Assert.Equal(ApplicationChannel.Referral, application.Channel);
    Assert.Equal(Later, application.AppliedAt);
    Assert.Equal("Hello", application.CoverLetter);
    Assert.Null(application.Outcome);
    Assert.Equal(Later, evt.OccurredAt);
  }

  [Fact]
  public void Apply_EmptyCvVersionId_Throws()
  {
    var vacancy = InStage(VacancyStage.Targeted);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(HypothesisOf(vacancy), Guid.Empty, ApplicationChannel.Website, Later, null));
  }

  [Fact]
  public void Apply_EmptyHypothesisId_Throws()
  {
    var vacancy = InStage(VacancyStage.Targeted);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(Guid.Empty, CvId, ApplicationChannel.Website, Later, null));
  }

  [Fact]
  public void Dismiss_Valid_SetsReasonAndCommentGoesToEventNote()
  {
    var vacancy = InStage(VacancyStage.Targeted);

    var evt = vacancy.Dismiss(DismissReason.WrongSalary, "too low", Later);

    Assert.Equal(DismissReason.WrongSalary, vacancy.DismissReason);
    Assert.Equal("too low", evt.Note);
  }

  [Theory]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public void Close_Valid_SetsApplicationOutcome(VacancyStage from)
  {
    var vacancy = InStage(from);

    vacancy.Close(ApplicationOutcome.Ghosted, Later);

    Assert.Equal(VacancyStage.Closed, vacancy.Stage);
    Assert.Equal(ApplicationOutcome.Ghosted, vacancy.Application!.Outcome);
  }

  [Theory]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public void MoveTo_ReopenFromClosed_ClearsOutcomeAndKeepsApplication(VacancyStage target)
  {
    var vacancy = InStage(VacancyStage.Closed);
    var application = vacancy.Application;

    vacancy.MoveTo(target, Later);

    Assert.Same(application, vacancy.Application);
    Assert.Null(vacancy.Application!.Outcome);
  }

  [Theory]
  [InlineData(VacancyStage.Inbox)]
  [InlineData(VacancyStage.Targeted)]
  [InlineData(VacancyStage.Preparing)]
  public void MoveTo_ReopenFromDismissed_ClearsDismissReason(VacancyStage target)
  {
    var vacancy = InStage(VacancyStage.Dismissed);
    Assert.NotNull(vacancy.DismissReason);

    vacancy.MoveTo(target, Later);

    Assert.Null(vacancy.DismissReason);
  }

  [Theory]
  [MemberData(nameof(NonUtcKinds))]
  public void MoveTo_NonUtcTime_Throws(DateTimeKind kind)
  {
    var vacancy = InStage(VacancyStage.Inbox);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.MoveTo(VacancyStage.Targeted, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [MemberData(nameof(NonUtcKinds))]
  public void Dismiss_NonUtcTime_Throws(DateTimeKind kind)
  {
    var vacancy = InStage(VacancyStage.Inbox);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Dismiss(DismissReason.Other, null, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [MemberData(nameof(NonUtcKinds))]
  public void Apply_NonUtcAppliedAt_Throws(DateTimeKind kind)
  {
    var vacancy = InStage(VacancyStage.Targeted);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Apply(
        HypothesisOf(vacancy), CvId, ApplicationChannel.Website, new DateTime(2026, 11, 12, 10, 0, 0, kind), null));
  }

  [Theory]
  [MemberData(nameof(NonUtcKinds))]
  public void Close_NonUtcTime_Throws(DateTimeKind kind)
  {
    var vacancy = InStage(VacancyStage.Applied);

    Assert.ThrowsAny<ArgumentException>(() =>
      vacancy.Close(ApplicationOutcome.Offer, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Fact]
  public void FullHappyPath_InboxToClosed_ProducesFiveOrderedEvents()
  {
    var vacancy = Create();
    var hypothesisId = AddMatch(vacancy);

    vacancy.MoveTo(VacancyStage.Targeted, Now);
    vacancy.MoveTo(VacancyStage.Preparing, Now.AddHours(1));
    vacancy.Apply(hypothesisId, CvId, ApplicationChannel.Website, Now.AddHours(2), null);
    vacancy.MoveTo(VacancyStage.Interviewing, Now.AddHours(3));
    vacancy.Close(ApplicationOutcome.Offer, Now.AddHours(4));

    var events = vacancy.Events.ToList();
    Assert.Equal(5, events.Count);
    VacancyStage[] stages =
    [
      VacancyStage.Inbox, VacancyStage.Targeted, VacancyStage.Preparing, VacancyStage.Applied,
      VacancyStage.Interviewing, VacancyStage.Closed,
    ];
    for (var i = 0; i < events.Count; i++)
    {
      Assert.Equal(stages[i], events[i].FromStage);
      Assert.Equal(stages[i + 1], events[i].ToStage);
      Assert.Equal(Now.AddHours(i), events[i].OccurredAt);
    }
  }
}
