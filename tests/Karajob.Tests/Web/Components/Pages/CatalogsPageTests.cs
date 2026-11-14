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
      Assert.Contains("Русский", cut.Markup);
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
    code.Change("de");

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

    cut.WaitForAssertion(() => Assert.Contains("Такая запись уже есть", cut.Markup));
    Assert.Equal(2, (await Service.ListAsync(CatalogKind.Role)).Count);
  }

  [Fact]
  public void Render_CatalogsPage_ShowsTabHeaders()
  {
    var cut = Context.Render<CatalogsPage>();

    cut.WaitForAssertion(() =>
    {
      Assert.Contains("Роли", cut.Markup);
      Assert.Contains("Локации", cut.Markup);
      Assert.Contains("Языки", cut.Markup);
    });
  }

  private static void TypeName(IRenderedComponent<CatalogList> cut, string name)
  {
    var input = cut.WaitForElement(".catalog-add-name input");
    input.Input(name);
    input.Change(name);
  }
}
