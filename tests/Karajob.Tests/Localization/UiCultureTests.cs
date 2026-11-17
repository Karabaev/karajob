using Bunit;
using Karajob.Domain.Applications;
using Karajob.Domain.Tracks;
using Karajob.Application.Metrics;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using Karajob.Web.Components.Tracks;
using Karajob.Web.Components.Vacancies;
using InboxPage = Karajob.Web.Components.Pages.Inbox;

namespace Karajob.Tests.Localization;

public class UiCultureTests : VacancyWebFixture
{
  [Fact]
  public void InboxPage_EnglishCulture_ShowsEnglishText()
  {
    using var _ = TestCulture.Use(TestCulture.English);

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Add manually", cut.Find(".inbox-add").TextContent);
      Assert.Contains("Inbox is empty for now.", cut.Find(".inbox-empty").TextContent);
    });
  }

  [Fact]
  public void InboxPage_RussianCulture_ShowsRussianText()
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    var cut = Context.Render<InboxPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Добавить вручную", cut.Find(".inbox-add").TextContent);
      Assert.Contains("В Inbox пока пусто.", cut.Find(".inbox-empty").TextContent);
    });
  }

  [Theory]
  [InlineData(VacancyStage.Inbox, "Inbox")]
  [InlineData(VacancyStage.Targeted, "Целевая")]
  [InlineData(VacancyStage.Preparing, "Подготовка")]
  [InlineData(VacancyStage.Applied, "Отклик")]
  [InlineData(VacancyStage.Interviewing, "Собеседования")]
  [InlineData(VacancyStage.Closed, "Закрыта")]
  [InlineData(VacancyStage.Dismissed, "Отклонена")]
  public void VacancyText_Stage_RussianCulture_ReturnsRussianLabel(VacancyStage stage, string expected)
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    Assert.Equal(expected, VacancyText.Stage(stage));
  }

  [Theory]
  [InlineData(ApplicationOutcome.Offer, "Оффер")]
  [InlineData(ApplicationOutcome.Rejected, "Отказ")]
  [InlineData(ApplicationOutcome.Ghosted, "Без ответа")]
  [InlineData(ApplicationOutcome.Withdrawn, "Отозван")]
  public void VacancyText_Outcome_RussianCulture_ReturnsRussianLabel(ApplicationOutcome outcome, string expected)
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    Assert.Equal(expected, VacancyText.Outcome(outcome));
  }

  [Theory]
  [InlineData(MetricVerdict.InsufficientData, "данных мало")]
  [InlineData(MetricVerdict.BelowThreshold, "ниже порога")]
  [InlineData(MetricVerdict.OnTrack, "в норме")]
  public void MetricsText_Verdict_RussianCulture_ReturnsRussianLabel(MetricVerdict verdict, string expected)
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    Assert.Equal(expected, MetricsText.Verdict(verdict));
  }

  [Theory]
  [InlineData(TrackStatus.Draft, "Черновик")]
  [InlineData(TrackStatus.Active, "Активен")]
  [InlineData(TrackStatus.Expanded, "Расширен")]
  [InlineData(TrackStatus.Paused, "На паузе")]
  [InlineData(TrackStatus.Disabled, "Отключён")]
  public void TrackText_Status_RussianCulture_ReturnsRussianLabel(TrackStatus status, string expected)
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    Assert.Equal(expected, TrackText.Status(status));
  }
}
