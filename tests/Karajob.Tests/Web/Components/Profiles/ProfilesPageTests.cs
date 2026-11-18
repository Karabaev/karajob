using AngleSharp.Dom;
using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Profiles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Profiles;

public class ProfilesPageTests : ProfileWebFixture
{
  private const string SelectFormSelector = "form[action='/profiles/select']";

  [Fact]
  public async Task Render_ListsOneSelectFormPerProfileWithName()
  {
    await Users.CreateAsync(new UserInput("Anna", "#26a69a"));

    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(SelectFormSelector).Count));
    var forms = cut.FindAll(SelectFormSelector);
    Assert.Contains(forms, f => f.TextContent.Contains("Owner"));
    Assert.Contains(forms, f => f.TextContent.Contains("Anna"));
  }

  [Fact]
  public void Render_SelectForm_PostsToSelectEndpoint()
  {
    var cut = RenderPage();

    var form = cut.WaitForElement(SelectFormSelector);

    Assert.Equal("post", form.GetAttribute("method")!.ToLowerInvariant());
  }

  [Fact]
  public async Task Render_SelectForm_HasHiddenProfileIdOfItsProfile()
  {
    var anna = await Users.CreateAsync(new UserInput("Anna", "#26a69a"));

    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(SelectFormSelector).Count));
    var annaForm = cut.FindAll(SelectFormSelector).Single(f => f.TextContent.Contains("Anna"));
    var field = annaForm.QuerySelector("input[name='profileId']");
    Assert.NotNull(field);
    Assert.Equal("hidden", field.GetAttribute("type"));
    Assert.Equal(anna.Id.ToString(), field.GetAttribute("value"));
    var ownerForm = cut.FindAll(SelectFormSelector).Single(f => f.TextContent.Contains("Owner"));
    Assert.Equal(SeedData.OwnerId.ToString(), ownerForm.QuerySelector("input[name='profileId']")!.GetAttribute("value"));
  }

  [Fact]
  public void Render_WithoutReturnUrl_HiddenReturnUrlIsRoot()
  {
    var cut = RenderPage();

    var form = cut.WaitForElement(SelectFormSelector);

    Assert.Equal("/", ReturnUrlOf(form));
  }

  [Fact]
  public void Render_WithReturnUrlQuery_HiddenReturnUrlCarriesIt()
  {
    Context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
      .NavigateTo("/profiles?returnUrl=%2Finbox");

    var cut = RenderPage();

    var form = cut.WaitForElement(SelectFormSelector);
    Assert.Equal("/inbox", ReturnUrlOf(form));
  }

  [Fact]
  public void ClickAddProfile_OpensNewProfileDialog()
  {
    var (cut, dialogs) = RenderPageWithDialogs();

    cut.WaitForElement(".add-profile-tile").Click();

    cut.WaitForAssertion(() => Assert.Contains("New profile", dialogs.Markup));
  }

  [Fact]
  public async Task Render_HasNoEditButtons()
  {
    await Users.CreateAsync(new UserInput("Anna", "#26a69a"));
    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(SelectFormSelector).Count));

    Assert.Empty(cut.FindAll("button[aria-label^='Edit profile']"));
  }

  [Fact]
  public async Task CreateViaDialog_AddsNewTile()
  {
    var (cut, dialogs) = RenderPageWithDialogs();
    cut.WaitForElement(".add-profile-tile").Click();
    dialogs.WaitForElement(".profile-name input").Input("Anna");

    dialogs.Find(".profile-save").Click();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal(2, cut.FindAll(SelectFormSelector).Count);
      Assert.Contains(cut.FindAll(SelectFormSelector), f => f.TextContent.Contains("Anna"));
    });
    Assert.Contains(await Users.ListAsync(), p => p.Name == "Anna");
  }

  private static string? ReturnUrlOf(IElement form)
  {
    return form.QuerySelector("input[name='returnUrl']")?.GetAttribute("value");
  }

  private (IRenderedComponent<ProfilesPage> Page, IRenderedComponent<MudDialogProvider> Dialogs) RenderPageWithDialogs()
  {
    Context.Render<MudPopoverProvider>();
    var dialogs = Context.Render<MudDialogProvider>();
    return (Context.Render<ProfilesPage>(), dialogs);
  }

  private IRenderedComponent<ProfilesPage> RenderPage()
  {
    return RenderPageWithDialogs().Page;
  }
}
