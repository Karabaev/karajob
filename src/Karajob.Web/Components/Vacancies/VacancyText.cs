using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Web.Resources;
using MudBlazor;

namespace Karajob.Web.Components.Vacancies;

public static class VacancyText
{
  public static string Stage(VacancyStage stage)
  {
    return stage switch
    {
      VacancyStage.Inbox => Localized.Get<VacancyStrings>("Stage.Inbox"),
      VacancyStage.Targeted => Localized.Get<VacancyStrings>("Stage.Targeted"),
      VacancyStage.Preparing => Localized.Get<VacancyStrings>("Stage.Preparing"),
      VacancyStage.Applied => Localized.Get<VacancyStrings>("Stage.Applied"),
      VacancyStage.Interviewing => Localized.Get<VacancyStrings>("Stage.Interviewing"),
      VacancyStage.Closed => Localized.Get<VacancyStrings>("Stage.Closed"),
      VacancyStage.Dismissed => Localized.Get<VacancyStrings>("Stage.Dismissed"),
      _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
  }

  public static Color StageColor(VacancyStage stage)
  {
    return stage switch
    {
      VacancyStage.Inbox => Color.Default,
      VacancyStage.Targeted => Color.Info,
      VacancyStage.Preparing => Color.Warning,
      VacancyStage.Applied => Color.Primary,
      VacancyStage.Interviewing => Color.Secondary,
      VacancyStage.Closed => Color.Success,
      VacancyStage.Dismissed => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
  }

  public static string EventType(ApplicationEventType type)
  {
    return type switch
    {
      ApplicationEventType.StageChanged => Localized.Get<VacancyStrings>("EventType.StageChanged"),
      ApplicationEventType.Screening => Localized.Get<VacancyStrings>("EventType.Screening"),
      ApplicationEventType.TechInterview => Localized.Get<VacancyStrings>("EventType.TechInterview"),
      ApplicationEventType.TestTask => Localized.Get<VacancyStrings>("EventType.TestTask"),
      ApplicationEventType.Rejected => Localized.Get<VacancyStrings>("EventType.Rejected"),
      ApplicationEventType.Offer => Localized.Get<VacancyStrings>("EventType.Offer"),
      ApplicationEventType.Note => Localized.Get<VacancyStrings>("EventType.Note"),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }

  public static string Dismiss(DismissReason reason)
  {
    return reason switch
    {
      DismissReason.WrongRole => Localized.Get<VacancyStrings>("Dismiss.WrongRole"),
      DismissReason.WrongLocation => Localized.Get<VacancyStrings>("Dismiss.WrongLocation"),
      DismissReason.WrongStack => Localized.Get<VacancyStrings>("Dismiss.WrongStack"),
      DismissReason.WrongSalary => Localized.Get<VacancyStrings>("Dismiss.WrongSalary"),
      DismissReason.Other => Localized.Get<VacancyStrings>("Dismiss.Other"),
      _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };
  }

  public static string Outcome(ApplicationOutcome outcome)
  {
    return outcome switch
    {
      ApplicationOutcome.Offer => Localized.Get<VacancyStrings>("Outcome.Offer"),
      ApplicationOutcome.Rejected => Localized.Get<VacancyStrings>("Outcome.Rejected"),
      ApplicationOutcome.Ghosted => Localized.Get<VacancyStrings>("Outcome.Ghosted"),
      ApplicationOutcome.Withdrawn => Localized.Get<VacancyStrings>("Outcome.Withdrawn"),
      _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };
  }

  public static string Channel(ApplicationChannel channel)
  {
    return channel switch
    {
      ApplicationChannel.Website => Localized.Get<VacancyStrings>("Channel.Website"),
      ApplicationChannel.Referral => Localized.Get<VacancyStrings>("Channel.Referral"),
      ApplicationChannel.Recruiter => Localized.Get<VacancyStrings>("Channel.Recruiter"),
      ApplicationChannel.ColdEmail => Localized.Get<VacancyStrings>("Channel.ColdEmail"),
      ApplicationChannel.Other => Localized.Get<VacancyStrings>("Channel.Other"),
      _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    };
  }
}
