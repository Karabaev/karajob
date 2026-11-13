using Bunit;
using Karajob.Web.Components.Hypotheses;

namespace Karajob.Tests.Web.Components.Hypotheses;

public class HypothesisPageTests : HypothesisWebFixture
{
  [Fact]
  public void TypeRole_OnNewPage_FillsNameWithRoleAndUpperCaseLanguage()
  {
    var cut = Context.Render<HypothesisPage>();

    cut.Find(".hypothesis-role input").Input("Unity Developer");

    cut.WaitForAssertion(() => Assert.Equal("Unity Developer RU", cut.Find(".hypothesis-name input").GetAttribute("value")));
  }

  [Fact]
  public void ChangeRole_AfterNameEditedManually_KeepsName()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.Find(".hypothesis-name input").Input("My Own Name");

    cut.Find(".hypothesis-role input").Input("Unity Developer");

    cut.WaitForAssertion(() => Assert.Equal("Unity Developer", cut.Find(".hypothesis-role input").GetAttribute("value")));
    Assert.Equal("My Own Name", cut.Find(".hypothesis-name input").GetAttribute("value"));
  }

  [Fact]
  public async Task Render_ExistingHypothesis_ShowsNameAndRoleDoesNotOverwriteIt()
  {
    var created = await Service.CreateAsync(Input("Gamedev EU"));
    var cut = Context.Render<HypothesisPage>(p => p.Add(c => c.Id, created.Id));
    cut.WaitForAssertion(() => Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value")));

    cut.Find(".hypothesis-role input").Input("Unreal Developer");

    cut.WaitForAssertion(() => Assert.Equal("Unreal Developer", cut.Find(".hypothesis-role input").GetAttribute("value")));
    Assert.Equal("Gamedev EU", cut.Find(".hypothesis-name input").GetAttribute("value"));
  }

  [Fact]
  public async Task Save_ValidNewHypothesis_PersistsHypothesis()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.Find(".hypothesis-role input").Input("Unity Developer");
    cut.WaitForAssertion(() => Assert.Equal("Unity Developer RU", cut.Find(".hypothesis-name input").GetAttribute("value")));

    cut.Find(".hypothesis-save").Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    IReadOnlyList<Karajob.Application.Hypotheses.HypothesisListItem> items = [];
    while (DateTime.UtcNow < deadline && items.Count == 0)
    {
      items = await Service.ListAsync();
      await Task.Delay(50);
    }

    var item = Assert.Single(items);
    Assert.Equal("Unity Developer RU", item.Name);
    Assert.Equal("Unity Developer", item.Role);
    Assert.Equal("ru", item.Language);
  }

  [Fact]
  public async Task Save_EmptyRole_ShowsRoleErrorAndPersistsNothing()
  {
    var cut = Context.Render<HypothesisPage>();
    cut.Find(".hypothesis-name input").Input("Some name");

    cut.Find(".hypothesis-save").Click();

    cut.WaitForAssertion(() => Assert.Contains("Укажите роль", cut.Markup));
    Assert.Empty(await Service.ListAsync());
  }
}
