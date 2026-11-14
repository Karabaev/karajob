using Bunit;
using Karajob.Application.Hypotheses;
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

    cut.Find(".hypothesis-save").Click();

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
  public async Task Save_NoRoleSelected_ShowsRoleErrorAndPersistsNothing()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.WaitForElement(".hypothesis-name input").Input("Some name");

    cut.Find(".hypothesis-save").Click();

    cut.WaitForAssertion(() => Assert.Contains("Укажите роль", cut.Markup));
    Assert.Empty(await Service.ListAsync());
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

  private static IRenderedComponent<MudSelect<Guid>> LocationsSelect(IRenderedComponent<HypothesisPage> cut)
  {
    return cut.FindComponents<MudSelect<Guid>>().Single(c => c.Instance.Class!.Contains("hypothesis-locations"));
  }
}
