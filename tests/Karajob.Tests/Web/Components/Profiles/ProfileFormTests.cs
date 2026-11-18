using Bunit;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Profiles;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Profiles;

public class ProfileFormTests : ProfileWebFixture
{
  private IRenderedComponent<MudPopoverProvider> _popovers = null!;

  [Fact]
  public void Render_CreateMode_ShowsOneSwatchPerPaletteColor()
  {
    var cut = RenderForm(null, out _);

    var swatches = cut.WaitForElements(".profile-color");

    Assert.Equal(ProfilePalette.Colors, swatches.Select(s => s.GetAttribute("aria-label")));
  }

  [Fact]
  public void Render_CreateMode_PreselectsFirstPaletteColor()
  {
    var cut = RenderForm(null, out _);

    var swatches = cut.WaitForElements(".profile-color");

    Assert.Equal("true", swatches[0].GetAttribute("aria-pressed"));
    Assert.All(swatches.Skip(1), s => Assert.Equal("false", s.GetAttribute("aria-pressed")));
  }

  [Fact]
  public void ClickSwatch_SelectsItAndDeselectsPrevious()
  {
    var cut = RenderForm(null, out _);
    cut.WaitForElements(".profile-color")[2].Click();

    var swatches = cut.FindAll(".profile-color");

    Assert.Equal("true", swatches[2].GetAttribute("aria-pressed"));
    Assert.Single(swatches, s => s.GetAttribute("aria-pressed") == "true");
  }

  [Fact]
  public async Task Save_CreateModeBlankName_ShowsErrorAndDoesNotRaiseOnSavedAndCreatesNothing()
  {
    var cut = RenderForm(null, out var saved);
    cut.WaitForElement(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".profile-name .mud-input-error")));
    Assert.Empty(saved);
    Assert.Single(await Users.ListAsync());
  }

  [Fact]
  public async Task Save_CreateModeValidName_CreatesProfileWithFirstPaletteColorAndRaisesOnSaved()
  {
    var cut = RenderForm(null, out var saved);
    cut.WaitForElement(".profile-name input").Input("Anna");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Single(saved));
    var dto = saved[0];
    Assert.Equal("Anna", dto.Name);
    Assert.Equal(ProfilePalette.Colors[0], dto.Color);
    Assert.Equal(dto, await Users.GetAsync(dto.Id));
    Assert.Equal(2, (await Users.ListAsync()).Count);
  }

  [Fact]
  public void Save_CreateModeAfterPickingSwatch_CreatesProfileWithPickedColor()
  {
    var cut = RenderForm(null, out var saved);
    cut.WaitForElement(".profile-name input").Input("Anna");
    cut.Find($".profile-color[aria-label='{ProfilePalette.Colors[3]}']").Click();
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Single(saved));
    Assert.Equal(ProfilePalette.Colors[3], saved[0].Color);
  }

  [Fact]
  public void Render_EditMode_PrefillsNameAndSelectsCurrentColor()
  {
    var cut = RenderForm(new UserDto(SeedData.OwnerId, "Owner", SeedData.OwnerColor), out _);

    var name = cut.WaitForElement(".profile-name input");

    Assert.Equal("Owner", name.GetAttribute("value"));
    var pressed = Assert.Single(cut.FindAll(".profile-color"), s => s.GetAttribute("aria-pressed") == "true");
    Assert.Equal(SeedData.OwnerColor, pressed.GetAttribute("aria-label"));
  }

  [Fact]
  public async Task Render_EditMode_ShowsProfileTimeZone()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0], "Asia/Tokyo"));
    var cut = RenderForm(created, out _);

    var input = cut.WaitForElement(".profile-time-zone input");

    Assert.Equal("(UTC+09:00) Asia/Tokyo", input.GetAttribute("value"));
  }

  [Fact]
  public async Task Save_EditMode_UpdatesProfileAndRaisesOnSaved()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0]));
    var cut = RenderForm(created, out var saved);
    cut.WaitForElement(".profile-name input").Input("Maria");
    cut.Find($".profile-color[aria-label='{ProfilePalette.Colors[1]}']").Click();
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Single(saved));
    var dto = saved[0];
    Assert.Equal(created.Id, dto.Id);
    Assert.Equal("Maria", dto.Name);
    Assert.Equal(ProfilePalette.Colors[1], dto.Color);
    Assert.Equal(dto, await Users.GetAsync(created.Id));
    Assert.Equal(2, (await Users.ListAsync()).Count);
  }

  [Fact]
  public async Task Save_EditModeBlankName_ShowsErrorAndDoesNotRaiseOnSavedAndKeepsStoredName()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0]));
    var cut = RenderForm(created, out var saved);
    cut.WaitForElement(".profile-name input").Input("");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".profile-name .mud-input-error")));
    Assert.Empty(saved);
    Assert.Equal("Anna", (await Users.GetAsync(created.Id))!.Name);
  }

  [Fact]
  public async Task Save_EditModeWithoutTouchingTimeZone_KeepsIt()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0], "Asia/Tokyo"));
    var cut = RenderForm(created, out var saved);
    cut.WaitForElement(".profile-name input").Input("Maria");
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Single(saved));
    Assert.Equal("Asia/Tokyo", saved[0].TimeZoneId);
  }

  [Fact]
  public async Task Save_PickedTimeZone_PersistsIt()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0]));
    var cut = RenderForm(created, out var saved);
    cut.WaitForElement(".profile-time-zone input");
    var timeZone = cut.FindComponent<MudAutocomplete<string>>();
    await cut.InvokeAsync(() => timeZone.Instance.ValueChanged.InvokeAsync("Asia/Tokyo"));
    cut.Find(".profile-save").Click();

    cut.WaitForAssertion(() => Assert.Single(saved));
    Assert.Equal("Asia/Tokyo", saved[0].TimeZoneId);
    Assert.Equal("Asia/Tokyo", (await Users.GetAsync(created.Id))!.TimeZoneId);
  }

  [Fact]
  public async Task OpenTimeZoneList_ProfileHasZone_OffersEveryZone()
  {
    var created = await Users.CreateAsync(new UserInput("Anna", ProfilePalette.Colors[0], "Asia/Tokyo"));
    var cut = RenderForm(created, out _);
    var input = cut.WaitForElement(".profile-time-zone input");

    input.Focus();

    _popovers.WaitForElements(".mud-list-item");
    Assert.True(_popovers.FindAll(".mud-list-item").Count > 50);
  }

  private IRenderedComponent<ProfileForm> RenderForm(UserDto? profile, out List<UserDto> saved)
  {
    var raised = new List<UserDto>();
    saved = raised;
    _popovers = Context.Render<MudPopoverProvider>();
    return Context.Render<ProfileForm>(p => p
      .Add(f => f.Profile, profile)
      .Add(f => f.OnSaved, EventCallback.Factory.Create<UserDto>(this, raised.Add)));
  }
}
