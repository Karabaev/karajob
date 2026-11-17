using Bunit;
using Karajob.Application.Goals;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Settings;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SettingsPage = Karajob.Web.Components.Pages.Settings;

namespace Karajob.Tests.Web.Components.Pages;

public class SettingsPageWellBeingTests : SettingsWebFixture
{
  [Fact]
  public void Render_DefaultGoal_ShowsDefaultWellBeingThreshold()
  {
    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() => Assert.Equal(2, Numeric(cut).Instance.Value));
  }

  [Fact]
  public async Task Render_StoredThreshold_ShowsStoredValue()
  {
    await StoreThresholdAsync(4);

    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() => Assert.Equal(4, Numeric(cut).Instance.Value));
  }

  [Fact]
  public async Task Save_ChangedThreshold_PersistsThroughSearchGoalService()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-wellbeing-threshold");
    await cut.InvokeAsync(() => Numeric(cut).Instance.ValueChanged.InvokeAsync(5));

    cut.Find(".settings-save").Click();

    var saved = await WaitForGoalAsync(g => g.WellBeingThreshold == 5);
    Assert.Equal(5, saved.WellBeingThreshold);
  }

  [Fact]
  public async Task Save_ThresholdChanged_KeepsOtherSettings()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-wellbeing-threshold");
    await cut.InvokeAsync(() => Numeric(cut).Instance.ValueChanged.InvokeAsync(3));

    cut.Find(".settings-save").Click();

    var saved = await WaitForGoalAsync(g => g.WellBeingThreshold == 3);
    Assert.Equal(10, saved.WeeklyApplicationTarget);
    Assert.Equal(30, saved.ConversionThresholdPermille);
    Assert.Equal(30, saved.MinSampleSize);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  public async Task Save_ThresholdOutOfRange_DoesNotSaveAndMarksFieldInvalid(int value)
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-wellbeing-threshold");
    await cut.InvokeAsync(() => Numeric(cut).Instance.ValueChanged.InvokeAsync(value));

    cut.Find(".settings-save").Click();

    cut.WaitForAssertion(() => Assert.True(Numeric(cut).Instance.Error));
    Assert.Equal(2, (await Service.GetAsync()).WellBeingThreshold);
  }

  private async Task StoreThresholdAsync(int threshold)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.WellBeingThreshold = threshold;
    await context.SaveChangesAsync();
  }

  private async Task<SearchGoalDetails> WaitForGoalAsync(Func<SearchGoalDetails, bool> predicate)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var details = await Service.GetAsync();
    while (DateTime.UtcNow < deadline && !predicate(details))
    {
      await Task.Delay(50);
      details = await Service.GetAsync();
    }

    Assert.True(predicate(details), "Settings were not persisted in time.");
    return details;
  }

  private static IRenderedComponent<MudNumericField<int>> Numeric(IRenderedComponent<SettingsPage> cut)
  {
    return cut.FindComponents<MudNumericField<int>>().Single(c => c.Instance.Class?.Contains("settings-wellbeing-threshold") == true);
  }
}
