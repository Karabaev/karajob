using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Web.Components.Profiles;
using Karajob.Web.Components.Pages;
using Karajob.Web.Components.Profiles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Pages;

public class ProfileSettingsTests : ProfileWebFixture
{
  [Fact]
  public async Task Render_PrefillsNameColorAndTimeZoneOfCurrentProfile()
  {
    await Users.UpdateAsync(SeedData.OwnerId, new UserInput("Owner", ProfilePalette.Colors[2], "Asia/Tokyo"));

    var cut = RenderPage();

    cut.WaitForAssertion(() =>
    {
      Assert.Equal("Owner", cut.Find(".profile-name input").GetAttribute("value"));
      var pressed = Assert.Single(cut.FindAll(".profile-color"), s => s.GetAttribute("aria-pressed") == "true");
      Assert.Equal(ProfilePalette.Colors[2], pressed.GetAttribute("aria-label"));
      Assert.Equal("(UTC+09:00) Asia/Tokyo", cut.Find(".profile-time-zone input").GetAttribute("value"));
    });
  }

  [Fact]
  public async Task Save_ChangedFields_PersistsNameColorAndTimeZone()
  {
    var cut = RenderPage();
    cut.WaitForElement(".profile-name input").Input("Boss");
    cut.Find($".profile-color[aria-label='{ProfilePalette.Colors[4]}']").Click();
    var timeZone = cut.FindComponent<MudAutocomplete<string>>();
    await cut.InvokeAsync(() => timeZone.Instance.ValueChanged.InvokeAsync("Asia/Tokyo"));
    cut.Find(".profile-save").Click();

    await cut.WaitForAssertionAsync(async () =>
    {
      var stored = (await Users.ListAsync()).Single(p => p.Id == SeedData.OwnerId);
      Assert.Equal("Boss", stored.Name);
      Assert.Equal(ProfilePalette.Colors[4], stored.Color);
      Assert.Equal("Asia/Tokyo", stored.TimeZoneId);
    });
  }

  [Fact]
  public async Task Save_BlankName_ShowsErrorAndDoesNotPersistNorRaiseProfileUpdated()
  {
    var raised = 0;
    CurrentUser.ProfileUpdated += () => raised++;
    var cut = RenderPage();
    cut.WaitForElement(".profile-name input").Input("");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".profile-name .mud-input-error")));
    Assert.Equal("Owner", (await Users.GetAsync(SeedData.OwnerId))!.Name);
    Assert.Equal(0, raised);
    Assert.Empty(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
  }

  [Fact]
  public void Save_ValidInput_RaisesProfileUpdated()
  {
    var raised = 0;
    CurrentUser.ProfileUpdated += () => raised++;
    var cut = RenderPage();
    cut.WaitForElement(".profile-name input").Input("Boss");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Equal(1, raised));
  }

  [Fact]
  public async Task Save_ChangedTimeZone_UpdatesCurrentUserTimeZone()
  {
    await Users.UpdateAsync(SeedData.OwnerId, new UserInput("Owner", SeedData.OwnerColor, "Asia/Tokyo"));
    var cut = RenderPage();
    cut.WaitForElement(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Equal(TimeSpan.FromHours(9), CurrentUser.TimeZone.BaseUtcOffset));
  }

  [Fact]
  public void Save_ValidInput_ShowsSuccessSnackbarSaved()
  {
    var snackbars = Context.Render<MudSnackbarProvider>();
    var cut = RenderPage();
    cut.WaitForElement(".profile-name input").Input("Boss");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() =>
    {
      var snackbar = Assert.Single(Context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
      Assert.Equal(Severity.Success, snackbar.Severity);
    });
    snackbars.WaitForAssertion(() => Assert.Contains("Saved", snackbars.Markup));
  }

  private IRenderedComponent<ProfileSettings> RenderPage()
  {
    Context.Render<MudPopoverProvider>();
    return Context.Render<ProfileSettings>();
  }
}
