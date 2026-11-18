using Bunit;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Web.Components.Vacancies;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using QuickAddPage = Karajob.Web.Components.Pages.QuickAdd;

namespace Karajob.Tests.Web.Components.Pages;

public class QuickAddPageTests : VacancyWebFixture
{
  [Fact]
  public void Render_UrlQuery_PrefillsUrlField()
  {
    var cut = RenderAt("?url=" + Uri.EscapeDataString("https://example.com/jobs/7"));

    var input = cut.WaitForElement(".quick-add-url input");

    Assert.Equal("https://example.com/jobs/7", input.GetAttribute("value"));
  }

  [Fact]
  public void Render_TextQueryWithLink_ExtractsUrlAndPutsLeftoverInNote()
  {
    var cut = RenderAt("?text=" + Uri.EscapeDataString("Senior Dev at Studio https://example.com/jobs/7"));

    var url = cut.WaitForElement(".quick-add-url input");

    Assert.Equal("https://example.com/jobs/7", url.GetAttribute("value"));
    Assert.Contains("Senior Dev at Studio", cut.Find(".quick-add-note textarea").TextContent);
  }

  [Fact]
  public void Render_TitleQuery_PrefillsTitleField()
  {
    var cut = RenderAt("?url=" + Uri.EscapeDataString("https://example.com/jobs/7") + "&title=Unity%20Dev");

    var title = cut.WaitForElement(".quick-add-title input");

    Assert.Equal("Unity Dev", title.GetAttribute("value"));
  }

  [Fact]
  public void Render_NoQuery_ShowsEmptyForm()
  {
    var cut = RenderAt(string.Empty);

    var url = cut.WaitForElement(".quick-add-url input");

    Assert.True(string.IsNullOrEmpty(url.GetAttribute("value")));
  }

  [Fact]
  public async Task Submit_PrefilledUrl_CreatesInboxVacancyAndNavigatesToInbox()
  {
    var cut = RenderAt("?url=" + Uri.EscapeDataString("https://example.com/jobs/7") + "&text=from%20a%20friend");
    cut.WaitForElement(".quick-add-submit");

    await cut.InvokeAsync(() => cut.Find(".quick-add-submit").Click());

    var item = Assert.Single(await Vacancies.ListAsync(VacancyStage.Inbox));
    Assert.Equal("https://example.com/jobs/7", item.Url);
    var found = await Vacancies.FindAsync(item.Id);
    Assert.Equal("from a friend", found!.Notes);
    var navigation = Context.Services.GetRequiredService<NavigationManager>();
    cut.WaitForAssertion(() => Assert.EndsWith("/inbox", navigation.Uri, StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task Submit_InvalidUrl_ShowsErrorDoesNotNavigateAndCreatesNothing()
  {
    var cut = RenderAt("?text=" + Uri.EscapeDataString("just words, no link"));
    cut.WaitForElement(".quick-add-submit");
    var navigation = Context.Services.GetRequiredService<NavigationManager>();
    var before = navigation.Uri;

    await cut.InvokeAsync(() => cut.Find(".quick-add-submit").Click());

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".quick-add-url .mud-input-error")));
    Assert.Equal(before, navigation.Uri);
    Assert.Empty(await Vacancies.ListAsync(VacancyStage.Inbox));
  }

  private IRenderedComponent<QuickAddPage> RenderAt(string query)
  {
    Context.Render<MudPopoverProvider>();
    var navigation = Context.Services.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/quick-add" + query);
    return Context.Render<QuickAddPage>();
  }
}
