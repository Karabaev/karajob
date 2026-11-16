using Bunit;
using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Hypotheses;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisPageTests : HypothesisWebFixture
{
  [Fact]
  public async Task SelectRole_OnNewPage_FillsNameWithRoleAndUpperCaseLanguage()
  {
    var cut = Context.Render<HypothesisPage>();

    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.Equal("Unity Developer RU", cut.Find(".hypothesis-name input").GetAttribute("value")));
  }

  [Fact]
  public async Task SelectLanguage_OnNewPageWithRole_UpdatesSuggestedName()
  {
    var cut = Context.Render<HypothesisPage>();
    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);

    await SelectLanguageAsync(cut, SeedData.EnglishLanguageId);

    cut.WaitForAssertion(() => Assert.Equal("Unity Developer EN", cut.Find(".hypothesis-name input").GetAttribute("value")));
  }

  [Fact]
  public async Task SelectRole_AfterNameEditedManually_KeepsName()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.WaitForElement(".hypothesis-name input").Input("My Own Name");

    await SelectRoleAsync(cut, SeedData.UnityDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.Equal<Guid?>(SeedData.UnityDeveloperRoleId, RoleSelect(cut).Instance.Value));
    Assert.Equal("My Own Name", cut.Find(".hypothesis-name input").GetAttribute("value"));
  }

  [Fact]
  public async Task Render_ExistingHypothesis_ShowsNameAndRoleChangeDoesNotOverwriteIt()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU"));
    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value")));
    Assert.Equal<Guid?>(SeedData.UnityDeveloperRoleId, RoleSelect(cut).Instance.Value);

    await SelectRoleAsync(cut, SeedData.UnrealDeveloperRoleId);

    cut.WaitForAssertion(() => Assert.Equal<Guid?>(SeedData.UnrealDeveloperRoleId, RoleSelect(cut).Instance.Value));
    Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value"));
  }

  [Fact]
  public async Task Render_ExistingHypothesis_ShowsStoredLanguageAndLocations()
  {
    var created = await Service.CreateAsync(
      Input("Gamedev EU") with { LanguageId = SeedData.EnglishLanguageId, LocationIds = [SeedData.EuLocationId, SeedData.SerbiaLocationId] });

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() =>
    {
      Assert.Equal<Guid?>(SeedData.EnglishLanguageId, LanguageSelect(cut).Instance.Value);
      Assert.Equal(
        new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(),
        LocationsSelect(cut).Instance.SelectedValues!.Order());
    });
  }

  [Fact]
  public async Task Save_ValidNewHypothesis_PersistsRoleLanguageAndLocations()
  {
    var cut = Context.Render<HypothesisPage>();
    await SelectRoleAsync(cut, SeedData.UnrealDeveloperRoleId);
    await SelectLanguageAsync(cut, SeedData.EnglishLanguageId);
    await cut.InvokeAsync(() => LocationsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.EuLocationId, SeedData.UsaLocationId]));
    cut.WaitForAssertion(() => Assert.Equal("Unreal Developer EN", cut.Find(".hypothesis-name input").GetAttribute("value")));

    ClickSave(cut);

    var deadline = DateTime.UtcNow.AddSeconds(5);
    IReadOnlyList<HypothesisListItem> items = [];
    while (DateTime.UtcNow < deadline && items.Count == 0)
    {
      items = await Service.ListAsync();
      await Task.Delay(50);
    }

    var item = Assert.Single(items);
    Assert.Equal("Unreal Developer EN", item.Name);
    Assert.Equal("Unreal Developer", item.Role);
    Assert.Equal("en", item.LanguageCode);
    var saved = await Service.FindAsync(item.Id);
    Assert.Equal(SeedData.UnrealDeveloperRoleId, saved!.RoleId);
    Assert.Equal(SeedData.EnglishLanguageId, saved.LanguageId);
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.UsaLocationId }.Order(), saved.LocationIds.Order());
  }

  [Fact]
  public void Render_NewHypothesis_ShowsPlatformsMultiSelectWithLabel()
  {
    var cut = Context.Render<HypothesisPage>();

    cut.WaitForAssertion(() =>
    {
      var select = PlatformsSelect(cut).Instance;
      Assert.True(select.MultiSelection);
      Assert.Equal("Платформы", select.Label);
    });
  }

  [Fact]
  public async Task Render_ExistingHypothesis_ShowsStoredPlatforms()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU") with { PlatformIds = [SeedData.PcPlatformId, SeedData.VrPlatformId] });

    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForAssertion(() => Assert.Equal(
      new[] { SeedData.PcPlatformId, SeedData.VrPlatformId }.Order(),
      PlatformsSelect(cut).Instance.SelectedValues!.Order()));
  }

  [Fact]
  public async Task Save_PlatformsSelectedOnNewPage_PersistsPlatforms()
  {
    var cut = Context.Render<HypothesisPage>();
    await SelectRoleAsync(cut, SeedData.UnrealDeveloperRoleId);
    await cut.InvokeAsync(() => PlatformsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.PcPlatformId, SeedData.WebPlatformId]));

    ClickSave(cut);

    var deadline = DateTime.UtcNow.AddSeconds(5);
    IReadOnlyList<HypothesisListItem> items = [];
    while (DateTime.UtcNow < deadline && items.Count == 0)
    {
      items = await Service.ListAsync();
      await Task.Delay(50);
    }

    var saved = await Service.FindAsync(Assert.Single(items).Id);
    Assert.Equal(new[] { SeedData.PcPlatformId, SeedData.WebPlatformId }.Order(), saved!.PlatformIds.Order());
  }

  [Fact]
  public async Task Save_PlatformsChangedOnExistingHypothesis_ReplacesPlatforms()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU") with { PlatformIds = [SeedData.PcPlatformId] });
    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal([SeedData.PcPlatformId], PlatformsSelect(cut).Instance.SelectedValues!));
    await cut.InvokeAsync(() => PlatformsSelect(cut).Instance.SelectedValuesChanged.InvokeAsync([SeedData.MobilePlatformId]));

    ClickSave(cut);

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var found = await Service.FindAsync(created.Id);
    while (DateTime.UtcNow < deadline && !found!.PlatformIds.Contains(SeedData.MobilePlatformId))
    {
      await Task.Delay(50);
      found = await Service.FindAsync(created.Id);
    }

    Assert.Equal([SeedData.MobilePlatformId], found!.PlatformIds);
  }

  [Fact]
  public async Task Save_NoRoleSelected_ShowsRoleErrorAndPersistsNothing()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.WaitForElement(".hypothesis-name input").Input("Some name");

    ClickSave(cut);

    cut.WaitForAssertion(() => Assert.Contains("Укажите роль", cut.Markup));
    Assert.Empty(await Service.ListAsync());
  }

  [Fact]
  public void Render_NewHypothesis_DoesNotShowCriteriaCard()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.WaitForElement(".hypothesis-name input");

    Assert.Empty(cut.FindAll(".hypothesis-criteria"));
  }

  [Fact]
  public async Task Render_DraftHypothesis_DoesNotShowCriteriaCard()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU"));
    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value")));

    Assert.Empty(cut.FindAll(".hypothesis-criteria"));
  }

  [Fact]
  public async Task Render_ActiveHypothesis_ShowsCriteriaCard()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU") with { Status = HypothesisStatus.Active });
    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));

    cut.WaitForElement(".hypothesis-criteria .criteria-market");
  }

  // The form can re-render between Find and Click after an input; a stale button then has no handler.
  private static void ClickSave(IRenderedComponent<HypothesisPage> cut)
  {
    cut.WaitForAssertion(() => cut.Find(".hypothesis-save").Click());
  }

  private static async Task SelectRoleAsync(IRenderedComponent<HypothesisPage> cut, Guid roleId)
  {
    cut.WaitForElement(".hypothesis-role");
    var select = RoleSelect(cut);
    await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(roleId));
  }

  private static async Task SelectLanguageAsync(IRenderedComponent<HypothesisPage> cut, Guid languageId)
  {
    cut.WaitForElement(".hypothesis-language");
    var select = LanguageSelect(cut);
    await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(languageId));
  }

  private static IRenderedComponent<MudSelect<Guid?>> RoleSelect(IRenderedComponent<HypothesisPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class!.Contains("hypothesis-role"));
  }

  private static IRenderedComponent<MudSelect<Guid?>> LanguageSelect(IRenderedComponent<HypothesisPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class!.Contains("hypothesis-language"));
  }

  private static IRenderedComponent<MudSelect<Guid>> PlatformsSelect(IRenderedComponent<HypothesisPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class!.Contains("hypothesis-platforms"));
  }

  private static IRenderedComponent<MudSelect<Guid>> LocationsSelect(IRenderedComponent<HypothesisPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class!.Contains("hypothesis-locations"));
  }
}
