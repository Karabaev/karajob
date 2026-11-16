using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Tests.Web.Components.Catalogs;
using Karajob.Web.Components.Catalogs;
using CatalogsPage = Karajob.Web.Components.Pages.Catalogs;

namespace Karajob.Tests.Web.Components.Pages;

// The delete flow goes through a MudBlazor message box; it is covered at the service level
// (CatalogServiceDeleteTests) so these tests do not depend on the dialog markup.
public class CatalogsPageTests : CatalogWebFixture
{
  [Fact]
  public void Render_Roles_ShowsSeededRoleNames()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Role));

    cut.WaitForAssertion(() =>
    {
      var names = cut.FindAll(".catalog-row .catalog-name").Select(e => e.TextContent.Trim()).ToList();
      Assert.Contains("Unity Developer", names);
      Assert.Contains("Unreal Developer", names);
      Assert.Equal(2, names.Count);
    });
  }

  [Fact]
  public void Render_Languages_ShowsNamesAndHasCodeInput()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Language));

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("English", cut.Markup);
      Assert.Contains("Russian", cut.Markup);
      Assert.NotEmpty(cut.FindAll(".catalog-add-code input"));
    });
  }

  [Fact]
  public void Render_Roles_HasNoCodeInput()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Role));

    cut.WaitForElement(".catalog-add-name input");

    Assert.Empty(cut.FindAll(".catalog-add-code input"));
  }

  [Fact]
  public async Task Add_NewRoleName_ShowsRowAndPersists()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Role));
    TypeName(cut, "Godot Developer");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll(".catalog-row .catalog-name"),
      e => e.TextContent.Trim() == "Godot Developer"));
    Assert.Contains(await Service.ListAsync(CatalogKind.Role), i => i.Name == "Godot Developer");
  }

  [Fact]
  public async Task Add_NewLanguageWithCode_ShowsRowAndPersists()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Language));
    TypeName(cut, "Deutsch");
    var code = cut.WaitForElement(".catalog-add-code input");
    code.Input("de");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains("Deutsch", cut.Markup));
    Assert.Contains(await Service.ListAsync(CatalogKind.Language), i => i.Name == "Deutsch" && i.Code == "de");
  }

  [Fact]
  public async Task Add_DuplicateName_ShowsValidationErrorAndPersistsNothing()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Role));
    TypeName(cut, "unity developer");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains("This entry already exists", cut.Markup));
    Assert.Equal(2, (await Service.ListAsync(CatalogKind.Role)).Count);
  }

  [Fact]
  public void Render_Platforms_ShowsSeededPlatformNamesWithoutCodeInput()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Platform));

    cut.WaitForAssertion(() =>
    {
      var names = cut.FindAll(".catalog-row .catalog-name").Select(e => e.TextContent.Trim()).ToList();
      Assert.Equal(["Consoles", "Mobile", "PC", "VR", "Web"], names.Order(StringComparer.OrdinalIgnoreCase));
    });
    Assert.Empty(cut.FindAll(".catalog-add-code input"));
  }

  [Fact]
  public async Task Add_NewPlatformName_ShowsRowAndPersists()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Platform));
    TypeName(cut, "Smart TV");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll(".catalog-row .catalog-name"),
      e => e.TextContent.Trim() == "Smart TV"));
    Assert.Contains(await Service.ListAsync(CatalogKind.Platform), i => i.Name == "Smart TV");
  }

  [Fact]
  public void Render_CatalogsPage_ShowsPlatformsTab()
  {
    var cut = Context.Render<CatalogsPage>();

    cut.WaitForAssertion(() => Assert.Contains("Platforms", cut.Markup));
  }

  [Fact]
  public void Render_Currencies_ShowsSeededNamesCodesAndCodeInput()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Currency));

    cut.WaitForAssertion(() =>
    {
      var names = cut.FindAll(".catalog-row .catalog-name").Select(e => e.TextContent.Trim()).ToList();
      Assert.Equal(["Euro", "Russian ruble", "US dollar"], names.Order(StringComparer.OrdinalIgnoreCase));
      var rows = cut.FindAll(".catalog-row").Select(r => r.TextContent).ToList();
      Assert.Contains(rows, r => r.Contains("US dollar") && r.Contains("USD"));
      Assert.Contains(rows, r => r.Contains("Euro") && r.Contains("EUR"));
      Assert.Contains(rows, r => r.Contains("Russian ruble") && r.Contains("RUB"));
      Assert.Contains(cut.FindAll("th"), h => h.TextContent.Trim() == "Code");
    });
    Assert.NotEmpty(cut.FindAll(".catalog-add-code input"));
  }

  [Fact]
  public async Task Add_NewCurrencyWithCode_ShowsRowAndPersistsUpperCaseCode()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Currency));
    TypeName(cut, "Фунт стерлингов");
    cut.WaitForElement(".catalog-add-code input").Input("gbp");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains("Фунт стерлингов", cut.Markup));
    Assert.Contains(await Service.ListAsync(CatalogKind.Currency), i => i.Name == "Фунт стерлингов" && i.Code == "GBP");
  }

  [Fact]
  public async Task Add_NewCurrencyWithoutCode_ShowsCodeErrorAndPersistsNothing()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Currency));
    TypeName(cut, "Фунт стерлингов");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains("Code must be 3 Latin letters", cut.Markup));
    Assert.Equal(3, (await Service.ListAsync(CatalogKind.Currency)).Count);
  }

  [Fact]
  public void Render_Engines_ShowsSeededEngineNamesWithoutCodeInput()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Engine));

    cut.WaitForAssertion(() =>
    {
      var names = cut.FindAll(".catalog-row .catalog-name").Select(e => e.TextContent.Trim()).ToList();
      Assert.Equal(["Cocos", "Defold", "Godot", "Proprietary", "Unity", "Unreal Engine"], names.Order(StringComparer.OrdinalIgnoreCase));
    });
    Assert.Empty(cut.FindAll(".catalog-add-code input"));
  }

  [Fact]
  public async Task Add_NewEngineName_ShowsRowAndPersists()
  {
    var cut = Context.Render<CatalogList>(p => p.Add(c => c.Kind, CatalogKind.Engine));
    TypeName(cut, "Phaser");

    cut.Find(".catalog-add").Click();

    cut.WaitForAssertion(() => Assert.Contains(
      cut.FindAll(".catalog-row .catalog-name"),
      e => e.TextContent.Trim() == "Phaser"));
    Assert.Contains(await Service.ListAsync(CatalogKind.Engine), i => i.Name == "Phaser");
  }

  [Fact]
  public void Render_CatalogsPage_ShowsCurrenciesAndEnginesTabs()
  {
    var cut = Context.Render<CatalogsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Currencies", cut.Markup);
      Assert.Contains("Engines", cut.Markup);
    });
  }

  [Fact]
  public void Render_CatalogsPage_ShowsTabHeaders()
  {
    var cut = Context.Render<CatalogsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Roles", cut.Markup);
      Assert.Contains("Locations", cut.Markup);
      Assert.Contains("Languages", cut.Markup);
    });
  }

  private static void TypeName(IRenderedComponent<CatalogList> cut, string name)
  {
    var input = cut.WaitForElement(".catalog-add-name input");
    input.Input(name);
  }
}
