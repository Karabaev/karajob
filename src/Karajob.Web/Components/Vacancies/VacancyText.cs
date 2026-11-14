using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using MudBlazor;

namespace Karajob.Web.Components.Vacancies;

public static class VacancyText
{
  public static string Stage(VacancyStage stage)
  {
    return stage switch
    {
      VacancyStage.Inbox => "Inbox",
      VacancyStage.Targeted => "Целевая",
      VacancyStage.Preparing => "Подготовка",
      VacancyStage.Applied => "Отклик",
      VacancyStage.Interviewing => "Собеседования",
      VacancyStage.Closed => "Закрыта",
      VacancyStage.Dismissed => "Отклонена",
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
      ApplicationEventType.StageChanged => "Смена стадии",
      ApplicationEventType.Screening => "Скрининг",
      ApplicationEventType.TechInterview => "Техническое собеседование",
      ApplicationEventType.TestTask => "Тестовое задание",
      ApplicationEventType.Rejected => "Отказ",
      ApplicationEventType.Offer => "Оффер",
      ApplicationEventType.Note => "Заметка",
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }

  public static string Dismiss(DismissReason reason)
  {
    return reason switch
    {
      DismissReason.WrongRole => "Не та роль",
      DismissReason.WrongLocation => "Не та локация",
      DismissReason.WrongStack => "Не тот стек",
      DismissReason.WrongSalary => "Не та зарплата",
      DismissReason.Other => "Другое",
      _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };
  }

  public static string Date(DateTime utc)
  {
    return utc.ToString(utc.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm");
  }
}
