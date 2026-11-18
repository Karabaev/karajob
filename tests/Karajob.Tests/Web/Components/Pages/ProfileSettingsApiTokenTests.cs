using AngleSharp.Dom;
using Bunit;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Profiles;
using Karajob.Web.Components.Pages;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Pages;

public class ProfileSettingsApiTokenTests : ProfileWebFixture
{
  private IRenderedComponent<MudDialogProvider> _dialogs = null!;

  [Fact]
  public void Render_ShowsGeneralAndApiTokenTabs()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      var tabs = cut.FindAll(".mud-tab").Select(t => t.TextContent.Trim()).ToList();
      Assert.Contains("General", tabs);
      Assert.Contains("API token", tabs);
    });
  }

  [Fact]
  public void Render_Default_ShowsGeneralTabWithProfileForm()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal("Owner", cut.Find(".profile-name input").GetAttribute("value"));
      Assert.NotEmpty(cut.FindAll(".profile-save"));
      Assert.Empty(cut.FindAll(".api-token-generate"));
    });
  }

  [Fact]
  public void Render_TabsAreVertical()
  {
    var cut = RenderPage();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-tabs-vertical")));
  }

  [Fact]
  public void ApiTokenTab_NoToken_ShowsGenerateButtonOnly()
  {
    var cut = RenderPage();

    OpenApiTokenTab(cut);

    Assert.NotEmpty(cut.FindAll(".api-token-generate"));
    Assert.Empty(cut.FindAll(".api-token-regenerate"));
    Assert.Empty(cut.FindAll(".api-token-revoke"));
    Assert.Empty(cut.FindAll(".api-token-value"));
    Assert.Empty(cut.FindAll(".api-token-created"));
  }

  [Fact]
  public async Task Generate_Clicked_ShowsPlaintextTokenWithPrefix()
  {
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-generate").Click();

    await cut.WaitForAssertionAsync(() => Assert.Matches("^kj_[0-9a-f]{64}$", TokenText(cut)));
  }

  [Fact]
  public async Task Generate_Clicked_StoresTokenThatMatchesShownValue()
  {
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-generate").Click();

    await cut.WaitForAssertionAsync(() => Assert.StartsWith("kj_", TokenText(cut)));
    var found = await Users.FindByApiTokenAsync(TokenText(cut));
    Assert.NotNull(found);
    Assert.Equal(SeedData.OwnerId, found.Id);
    Assert.True((await Users.GetApiTokenStatusAsync(SeedData.OwnerId)).Exists);
  }

  [Fact]
  public async Task Generate_Clicked_ShowsCreatedDateAndRegenerateRevokeButtons()
  {
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-generate").Click();

    await cut.WaitForAssertionAsync(() =>
    {
      Assert.NotEmpty(cut.FindAll(".api-token-created"));
      Assert.NotEmpty(cut.FindAll(".api-token-regenerate"));
      Assert.NotEmpty(cut.FindAll(".api-token-revoke"));
      Assert.Empty(cut.FindAll(".api-token-generate"));
    });
  }

  [Fact]
  public async Task Render_ExistingToken_ShowsOnlyCreationDateWithoutTokenValue()
  {
    var token = await Users.IssueApiTokenAsync(SeedData.OwnerId);

    var cut = RenderPage();
    OpenApiTokenTab(cut);

    Assert.NotEmpty(cut.FindAll(".api-token-created"));
    Assert.Empty(cut.FindAll(".api-token-value"));
    Assert.DoesNotContain(token, cut.Markup);
    Assert.NotEmpty(cut.FindAll(".api-token-regenerate"));
    Assert.NotEmpty(cut.FindAll(".api-token-revoke"));
    Assert.Empty(cut.FindAll(".api-token-generate"));
  }

  [Fact]
  public async Task Generate_ThenPageRerendered_NoLongerShowsTokenValue()
  {
    var cut = RenderPage();
    OpenApiTokenTab(cut);
    cut.Find(".api-token-generate").Click();
    await cut.WaitForAssertionAsync(() => Assert.StartsWith("kj_", TokenText(cut)));
    var token = TokenText(cut);

    var reopened = RenderPage();
    OpenApiTokenTab(reopened);

    Assert.Empty(reopened.FindAll(".api-token-value"));
    Assert.DoesNotContain(token, reopened.Markup);
    Assert.NotEmpty(reopened.FindAll(".api-token-created"));
  }

  [Fact]
  public async Task Regenerate_Clicked_AsksForConfirmationAndKeepsOldTokenUntilConfirmed()
  {
    var oldToken = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-regenerate").Click();

    _dialogs.WaitForElement(".mud-message-box__yes-button");
    Assert.NotNull(await Users.FindByApiTokenAsync(oldToken));
    Assert.Empty(cut.FindAll(".api-token-value"));
  }

  [Fact]
  public async Task Regenerate_Confirmed_InvalidatesOldTokenAndShowsNewOne()
  {
    var oldToken = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-regenerate").Click();
    _dialogs.WaitForElement(".mud-message-box__yes-button").Click();

    await cut.WaitForAssertionAsync(() => Assert.Matches("^kj_[0-9a-f]{64}$", TokenText(cut)));
    var newToken = TokenText(cut);
    Assert.NotEqual(oldToken, newToken);
    Assert.Null(await Users.FindByApiTokenAsync(oldToken));
    Assert.Equal(SeedData.OwnerId, (await Users.FindByApiTokenAsync(newToken))!.Id);
  }

  [Fact]
  public async Task Regenerate_Cancelled_KeepsOldToken()
  {
    var oldToken = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-regenerate").Click();
    _dialogs.WaitForElement(".mud-message-box__cancel-button").Click();
    await Task.Delay(300);

    Assert.NotNull(await Users.FindByApiTokenAsync(oldToken));
    Assert.Empty(cut.FindAll(".api-token-value"));
  }

  [Fact]
  public async Task Revoke_Clicked_AsksForConfirmationAndKeepsTokenUntilConfirmed()
  {
    var token = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-revoke").Click();

    _dialogs.WaitForElement(".mud-message-box__yes-button");
    Assert.NotNull(await Users.FindByApiTokenAsync(token));
    Assert.True((await Users.GetApiTokenStatusAsync(SeedData.OwnerId)).Exists);
  }

  [Fact]
  public async Task Revoke_Confirmed_RemovesTokenAndShowsGenerateButton()
  {
    var token = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-revoke").Click();
    _dialogs.WaitForElement(".mud-message-box__yes-button").Click();

    await cut.WaitForAssertionAsync(() =>
    {
      Assert.NotEmpty(cut.FindAll(".api-token-generate"));
      Assert.Empty(cut.FindAll(".api-token-revoke"));
      Assert.Empty(cut.FindAll(".api-token-created"));
    });
    Assert.Null(await Users.FindByApiTokenAsync(token));
    Assert.False((await Users.GetApiTokenStatusAsync(SeedData.OwnerId)).Exists);
  }

  [Fact]
  public async Task Revoke_Cancelled_KeepsToken()
  {
    var token = await Users.IssueApiTokenAsync(SeedData.OwnerId);
    var cut = RenderPage();
    OpenApiTokenTab(cut);

    cut.Find(".api-token-revoke").Click();
    _dialogs.WaitForElement(".mud-message-box__cancel-button").Click();
    await Task.Delay(300);

    Assert.NotNull(await Users.FindByApiTokenAsync(token));
    Assert.NotEmpty(cut.FindAll(".api-token-revoke"));
  }

  private static string TokenText(IRenderedComponent<ProfileSettings> cut)
  {
    var element = cut.Find(".api-token-value");
    return (element.QuerySelector("input")?.GetAttribute("value") ?? element.TextContent).Trim();
  }

  private static void OpenApiTokenTab(IRenderedComponent<ProfileSettings> cut)
  {
    cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-tab"), IsApiTokenTab));
    cut.FindAll(".mud-tab").First(IsApiTokenTab).Click();
    cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".profile-save")));
  }

  private static bool IsApiTokenTab(IElement tab)
  {
    return tab.TextContent.Trim() == "API token";
  }

  private IRenderedComponent<ProfileSettings> RenderPage()
  {
    Context.Render<MudPopoverProvider>();
    _dialogs = Context.Render<MudDialogProvider>();
    return Context.Render<ProfileSettings>();
  }
}
