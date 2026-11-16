using Bunit;
using Karajob.Application.Goals;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Settings;
using MudBlazor;
using SettingsPage = Karajob.Web.Components.Pages.Settings;

namespace Karajob.Tests.Web.Components.Pages;

public class SettingsPageTests : SettingsWebFixture
{
  [Fact]
  public void Render_DefaultGoal_ShowsDefaultThresholds()
  {
    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal(10, Numeric<int>(cut, "settings-weekly-target").Instance.Value);
      Assert.Equal(10, Numeric<int>(cut, "settings-market-threshold").Instance.Value);
      Assert.Equal(3m, Numeric<decimal>(cut, "settings-conversion-threshold").Instance.Value);
      Assert.Equal(30, Numeric<int>(cut, "settings-min-sample").Instance.Value);
      Assert.Equal(14, Numeric<int>(cut, "settings-maturation-days").Instance.Value);
    });
  }

  [Fact]
  public async Task Render_StoredGoal_ShowsStoredValues()
  {
    await Service.UpdateAsync(new SearchGoalInput(
      SeedData.UnrealDeveloperRoleId,
      [SeedData.EuLocationId, SeedData.SerbiaLocationId],
      [],
      [],
      "Gamedev",
      "Remote",
      17,
      new DateOnly(2027, 3, 1),
      6000,
      "EUR",
      22,
      45,
      60,
      21));

    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal<Guid?>(SeedData.UnrealDeveloperRoleId, RoleSelect(cut).Instance.Value);
      Assert.Equal(
        new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(),
        LocationsSelect(cut).Instance.SelectedValues!.Order());
      Assert.Equal("Gamedev", Text(cut, "settings-industries").Instance.Value);
      Assert.Equal("Remote", Text(cut, "settings-must-have").Instance.Value);
      Assert.Equal(17, Numeric<int>(cut, "settings-weekly-target").Instance.Value);
      Assert.Equal(new DateTime(2027, 3, 1), DeadlinePicker(cut).Instance.Date);
      Assert.Equal(6000L, Numeric<long?>(cut, "settings-income").Instance.Value);
      Assert.Equal("EUR", Text(cut, "settings-currency").Instance.Value);
      Assert.Equal(22, Numeric<int>(cut, "settings-market-threshold").Instance.Value);
      Assert.Equal(4.5m, Numeric<decimal>(cut, "settings-conversion-threshold").Instance.Value);
      Assert.Equal(60, Numeric<int>(cut, "settings-min-sample").Instance.Value);
      Assert.Equal(21, Numeric<int>(cut, "settings-maturation-days").Instance.Value);
    });
  }

  [Fact]
  public async Task Save_ChangedNumbers_PersistsWeeklyTargetAndConversionAsPermille()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-weekly-target");
    await SetNumericAsync<int>(cut, "settings-weekly-target", 15);
    await SetNumericAsync<decimal>(cut, "settings-conversion-threshold", 4.5m);

    cut.Find(".settings-save").Click();

    var saved = await WaitForGoalAsync(g => g.WeeklyApplicationTarget == 15);
    Assert.Equal(45, saved.ConversionThresholdPermille);
    Assert.Equal(10, saved.MarketThresholdPerWeek);
    Assert.Equal(30, saved.MinSampleSize);
    Assert.Equal(14, saved.MaturationDays);
  }

  [Fact]
  public async Task Save_AllFieldsChanged_PersistsEveryField()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-role");
    await cut.InvokeAsync(() => RoleSelect(cut).Instance.ValueChanged.InvokeAsync(SeedData.UnityDeveloperRoleId));
    await cut.InvokeAsync(() => LocationsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.EuLocationId, SeedData.UsaLocationId]));
    await SetTextAsync(cut, "settings-industries", " Gamedev ");
    await SetTextAsync(cut, "settings-must-have", "Remote");
    await SetNumericAsync<int>(cut, "settings-weekly-target", 12);
    await cut.InvokeAsync(() => DeadlinePicker(cut).Instance.DateChanged.InvokeAsync(new DateTime(2027, 2, 1)));
    await SetNumericAsync<long?>(cut, "settings-income", 5000);
    await SetTextAsync(cut, "settings-currency", "usd");
    await SetNumericAsync<int>(cut, "settings-market-threshold", 25);
    await SetNumericAsync<decimal>(cut, "settings-conversion-threshold", 5m);
    await SetNumericAsync<int>(cut, "settings-min-sample", 40);
    await SetNumericAsync<int>(cut, "settings-maturation-days", 30);

    cut.Find(".settings-save").Click();

    var saved = await WaitForGoalAsync(g => g.WeeklyApplicationTarget == 12);
    Assert.Equal(SeedData.UnityDeveloperRoleId, saved.RoleId);
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.UsaLocationId }.Order(), saved.LocationIds.Order());
    Assert.Equal("Gamedev", saved.Industries);
    Assert.Equal("Remote", saved.MustHave);
    Assert.Equal(new DateOnly(2027, 2, 1), saved.Deadline);
    Assert.Equal(5000, saved.DesiredMonthlyIncome);
    Assert.Equal("USD", saved.IncomeCurrency);
    Assert.Equal(25, saved.MarketThresholdPerWeek);
    Assert.Equal(50, saved.ConversionThresholdPermille);
    Assert.Equal(40, saved.MinSampleSize);
    Assert.Equal(30, saved.MaturationDays);
  }

  [Fact]
  public void Render_DefaultGoal_ShowsPlatformsMultiSelectWithLabelAndNoSelection()
  {
    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() =>
    {
      var select = PlatformsSelect(cut).Instance;
      Assert.True(select.MultiSelection);
      Assert.Equal("Платформы", select.Label);
      Assert.Empty(select.SelectedValues!);
    });
  }

  [Fact]
  public async Task Render_StoredGoalWithPlatforms_ShowsStoredPlatforms()
  {
    var goal = await Service.GetAsync();
    await Service.UpdateAsync(new SearchGoalInput(
      goal.RoleId,
      goal.LocationIds,
      [SeedData.ConsolesPlatformId, SeedData.PcPlatformId],
      goal.EngineIds,
      goal.Industries,
      goal.MustHave,
      goal.WeeklyApplicationTarget,
      goal.Deadline,
      goal.DesiredMonthlyIncome,
      goal.IncomeCurrency,
      goal.MarketThresholdPerWeek,
      goal.ConversionThresholdPermille,
      goal.MinSampleSize,
      goal.MaturationDays));

    var cut = Context.Render<SettingsPage>();

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { SeedData.ConsolesPlatformId, SeedData.PcPlatformId }.Order(),
      PlatformsSelect(cut).Instance.SelectedValues!.Order()));
  }

  [Fact]
  public async Task Save_PlatformsSelected_PersistsPlatforms()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-role");
    await cut.InvokeAsync(() => PlatformsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.VrPlatformId, SeedData.WebPlatformId]));

    cut.Find(".settings-save").Click();

    var saved = await WaitForGoalAsync(g => g.PlatformIds.Count == 2);
    Assert.Equal(new[] { SeedData.VrPlatformId, SeedData.WebPlatformId }.Order(), saved.PlatformIds.Order());
  }

  [Fact]
  public async Task Save_MinSampleZero_DoesNotSaveAndMarksFieldInvalid()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-min-sample");
    await SetNumericAsync<int>(cut, "settings-weekly-target", 15);
    await SetNumericAsync<int>(cut, "settings-min-sample", 0);

    cut.Find(".settings-save").Click();

    cut.WaitForAssertion(() => Assert.True(Numeric<int>(cut, "settings-min-sample").Instance.Error));
    var stored = await Service.GetAsync();
    Assert.Equal(10, stored.WeeklyApplicationTarget);
    Assert.Equal(30, stored.MinSampleSize);
  }

  [Fact]
  public async Task Save_IncomeWithoutCurrency_DoesNotSave()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-income");
    await SetNumericAsync<long?>(cut, "settings-income", 5000);

    cut.Find(".settings-save").Click();

    cut.WaitForAssertion(() => Assert.True(Text(cut, "settings-currency").Instance.Error));
    Assert.Null((await Service.GetAsync()).DesiredMonthlyIncome);
  }

  [Fact]
  public async Task Save_CurrencyWithoutIncome_DoesNotSaveAndMarksIncomeInvalid()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-currency");
    await SetNumericAsync<int>(cut, "settings-weekly-target", 15);
    await SetTextAsync(cut, "settings-currency", "EUR");

    cut.Find(".settings-save").Click();

    cut.WaitForAssertion(() => Assert.True(Numeric<long?>(cut, "settings-income").Instance.Error));
    Assert.Equal("EUR", Text(cut, "settings-currency").Instance.Value);
    var stored = await Service.GetAsync();
    Assert.Equal(10, stored.WeeklyApplicationTarget);
    Assert.Null(stored.IncomeCurrency);
  }

  [Fact]
  public async Task Save_HugeConversionPercent_ShowsValidationErrorInsteadOfCrashing()
  {
    var cut = Context.Render<SettingsPage>();
    cut.WaitForElement(".settings-conversion-threshold");
    await SetNumericAsync<decimal>(cut, "settings-conversion-threshold", 300_000_000m);

    cut.Find(".settings-save").Click();

    cut.WaitForAssertion(() => Assert.True(Numeric<decimal>(cut, "settings-conversion-threshold").Instance.Error));
    Assert.Equal(30, (await Service.GetAsync()).ConversionThresholdPermille);
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

  private static async Task SetNumericAsync<T>(IRenderedComponent<SettingsPage> cut, string cssClass, T value)
  {
    var field = Numeric<T>(cut, cssClass);
    await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
  }

  private static async Task SetTextAsync(IRenderedComponent<SettingsPage> cut, string cssClass, string value)
  {
    var field = Text(cut, cssClass);
    await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
  }

  private static IRenderedComponent<MudNumericField<T>> Numeric<T>(IRenderedComponent<SettingsPage> cut, string cssClass)
  {
    return cut.FindComponents<MudNumericField<T>>().Single(c => c.Instance.Class?.Contains(cssClass) == true);
  }

  private static IRenderedComponent<MudTextField<string>> Text(IRenderedComponent<SettingsPage> cut, string cssClass)
  {
    return cut.FindComponents<MudTextField<string>>().Single(c => c.Instance.Class?.Contains(cssClass) == true);
  }

  private static IRenderedComponent<MudSelect<Guid?>> RoleSelect(IRenderedComponent<SettingsPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class?.Contains("settings-role") == true);
  }

  private static IRenderedComponent<MudSelect<Guid>> PlatformsSelect(IRenderedComponent<SettingsPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class?.Contains("settings-platforms") == true);
  }

  private static IRenderedComponent<MudSelect<Guid>> LocationsSelect(IRenderedComponent<SettingsPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class?.Contains("settings-locations") == true);
  }

  private static IRenderedComponent<MudDatePicker> DeadlinePicker(IRenderedComponent<SettingsPage> cut)
  {
    return cut.FindComponents<MudDatePicker>().Single(c => c.Instance.Class?.Contains("settings-deadline") == true);
  }
}
