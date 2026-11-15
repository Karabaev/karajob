using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Profiles;
using Karajob.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Layout;

public class ProfileMenuTests : ProfileWebFixture
{
  private const string ActivatorSelector = "button[aria-label='Профиль: Owner']";
  private const string SelectFormSelector = "form[action='/profiles/select']";

  [Fact]
  public void Render_HasActivatorForCurrentProfileWithAvatar()
  {
    var (cut, _) = RenderMenu();

    var activator = cut.WaitForElement(ActivatorSelector);

    Assert.Contains("O", activator.TextContent);
    Assert.Contains(SeedData.OwnerColor, activator.InnerHtml, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Render_Closed_DoesNotShowSwitchForms()
  {
    var (_, popover) = RenderMenu();

    Assert.Empty(popover.FindAll(SelectFormSelector));
  }

  [Fact]
  public async Task Open_ListsOtherProfilesEachInSelectFormWithHiddenProfileId()
  {
    var anna = await Users.CreateAsync(new UserInput("Anna", "#26a69a"));
    var maria = await Users.CreateAsync(new UserInput("Maria", "#ef5350"));
    var (cut, popover) = RenderMenu();

    cut.WaitForElement(ActivatorSelector).Click();

    popover.WaitForAssertion(() => Assert.Equal(2, popover.FindAll(SelectFormSelector).Count));
    var ids = popover.FindAll($"{SelectFormSelector} input[name='profileId']").Select(i => i.GetAttribute("value"));
    Assert.Equivalent(new[] { anna.Id.ToString(), maria.Id.ToString() }, ids);
    Assert.All(popover.FindAll(SelectFormSelector), f => Assert.Equal("post", f.GetAttribute("method")!.ToLowerInvariant()));
  }

  [Fact]
  public async Task Open_DoesNotListCurrentProfileAsSwitchTarget()
  {
    await Users.CreateAsync(new UserInput("Anna", "#26a69a"));
    var (cut, popover) = RenderMenu();

    cut.WaitForElement(ActivatorSelector).Click();

    popover.WaitForAssertion(() => Assert.NotEmpty(popover.FindAll(SelectFormSelector)));
    Assert.DoesNotContain(
      popover.FindAll($"{SelectFormSelector} input[name='profileId']"),
      i => i.GetAttribute("value") == SeedData.OwnerId.ToString());
  }

  [Fact]
  public async Task Open_SelectFormsCarryCurrentRelativeUrlAsReturnUrl()
  {
    await Users.CreateAsync(new UserInput("Anna", "#26a69a"));
    Context.Services.GetRequiredService<NavigationManager>().NavigateTo("/inbox");
    var (cut, popover) = RenderMenu();

    cut.WaitForElement(ActivatorSelector).Click();

    popover.WaitForAssertion(() =>
    {
      var returnUrl = Assert.Single(popover.FindAll($"{SelectFormSelector} input[name='returnUrl']"));
      Assert.Equal("/inbox", returnUrl.GetAttribute("value"));
    });
  }

  [Fact]
  public void Open_HasLinkToAllProfiles()
  {
    var (cut, popover) = RenderMenu();

    cut.WaitForElement(ActivatorSelector).Click();

    popover.WaitForAssertion(() =>
    {
      var link = popover.FindAll("a").Single(a => a.TextContent.Trim() == "Все профили");
      Assert.Equal("/profiles", link.GetAttribute("href"));
    });
  }

  private (IRenderedComponent<ProfileMenu> Menu, IRenderedComponent<MudPopoverProvider> Popover) RenderMenu()
  {
    var popover = Context.Render<MudPopoverProvider>();
    return (Context.Render<ProfileMenu>(), popover);
  }
}
