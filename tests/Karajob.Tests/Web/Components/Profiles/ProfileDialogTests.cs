using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Profiles;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Profiles;

public class ProfileDialogTests : ProfileWebFixture
{
  private static DialogParameters<ProfileDialog> CreateMode()
  {
    return new DialogParameters<ProfileDialog>();
  }

  private static DialogParameters<ProfileDialog> EditMode(UserDto profile)
  {
    return new DialogParameters<ProfileDialog> { { d => d.Profile, profile } };
  }

  [Fact]
  public async Task Render_CreateMode_ShowsOneSwatchPerPaletteColor()
  {
    var (provider, _) = await ShowDialogAsync(CreateMode());

    var swatches = provider.WaitForElements(".profile-color");

    Assert.Equal(ProfilePalette.Colors, swatches.Select(s => s.GetAttribute("aria-label")));
  }

  [Fact]
  public async Task Render_CreateMode_PreselectsFirstPaletteColor()
  {
    var (provider, _) = await ShowDialogAsync(CreateMode());

    var swatches = provider.WaitForElements(".profile-color");

    Assert.Equal("true", swatches[0].GetAttribute("aria-pressed"));
    Assert.All(swatches.Skip(1), s => Assert.Equal("false", s.GetAttribute("aria-pressed")));
  }

  [Fact]
  public async Task ClickSwatch_SelectsItAndDeselectsPrevious()
  {
    var (provider, _) = await ShowDialogAsync(CreateMode());
    provider.WaitForElements(".profile-color")[2].Click();

    var swatches = provider.FindAll(".profile-color");

    Assert.Equal("true", swatches[2].GetAttribute("aria-pressed"));
    Assert.Single(swatches, s => s.GetAttribute("aria-pressed") == "true");
  }

  [Fact]
  public async Task Save_CreateModeBlankName_ShowsErrorAndStaysOpenAndCreatesNothing()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-save").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".profile-name .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Single(await Users.ListAsync());
  }

  [Fact]
  public async Task Save_CreateModeValidName_CreatesProfileWithFirstPaletteColorAndClosesWithDto()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-name input").Input("Anna");
    provider.Find(".profile-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.False(result!.Canceled);
    var dto = Assert.IsType<UserDto>(result.Data);
    Assert.Equal("Anna", dto.Name);
    Assert.Equal(ProfilePalette.Colors[0], dto.Color);
    Assert.Equal(dto, await Users.GetAsync(dto.Id));
  }

  [Fact]
  public async Task Save_CreateModeAfterPickingSwatch_CreatesProfileWithPickedColor()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-name input").Input("Anna");
    provider.Find($".profile-color[aria-label='{ProfilePalette.Colors[3]}']").Click();
    provider.Find(".profile-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.Equal(ProfilePalette.Colors[3], Assert.IsType<UserDto>(result!.Data).Color);
  }

  [Fact]
  public async Task Render_EditMode_PrefillsNameAndSelectsCurrentColor()
  {
    var (provider, _) = await ShowDialogAsync(EditMode(new UserDto(SeedData.OwnerId, "Owner", SeedData.OwnerColor)));

    var name = provider.WaitForElement(".profile-name input");

    Assert.Equal("Owner", name.GetAttribute("value"));
    var pressed = Assert.Single(provider.FindAll(".profile-color"), s => s.GetAttribute("aria-pressed") == "true");
    Assert.Equal(SeedData.OwnerColor, pressed.GetAttribute("aria-label"));
  }

  [Fact]
  public async Task Save_EditMode_UpdatesProfileAndClosesWithDto()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0]));
    var (provider, reference) = await ShowDialogAsync(EditMode(created));
    provider.WaitForElement(".profile-name input").Input("Maria");
    provider.Find($".profile-color[aria-label='{ProfilePalette.Colors[1]}']").Click();
    provider.Find(".profile-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    var dto = Assert.IsType<UserDto>(result!.Data);
    Assert.Equal(created.Id, dto.Id);
    Assert.Equal("Maria", dto.Name);
    Assert.Equal(ProfilePalette.Colors[1], dto.Color);
    Assert.Equal(dto, await Users.GetAsync(created.Id));
    Assert.Equal(2, (await Users.ListAsync()).Count);
  }

  [Fact]
  public async Task Save_EditModeBlankName_ShowsErrorAndStaysOpenAndKeepsStoredName()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0]));
    var (provider, reference) = await ShowDialogAsync(EditMode(created));
    provider.WaitForElement(".profile-name input").Input("");
    provider.Find(".profile-save").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".profile-name .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Equal("Anna", (await Users.GetAsync(created.Id))!.Name);
  }
}
