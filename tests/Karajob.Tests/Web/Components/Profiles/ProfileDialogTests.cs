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

  [Fact]
  public async Task Render_ShowsProfileForm()
  {
    var (provider, _) = await ShowDialogAsync(CreateMode());

    provider.WaitForElement(".profile-name input");
    Assert.NotEmpty(provider.FindAll(".profile-color"));
    Assert.NotEmpty(provider.FindAll(".profile-time-zone"));
  }

  [Fact]
  public async Task Save_ValidName_CreatesProfileAndClosesWithDto()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-name input").Input("Anna");
    provider.Find(".profile-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.False(result!.Canceled);
    var dto = Assert.IsType<UserDto>(result.Data);
    Assert.Equal("Anna", dto.Name);
    Assert.Equal(dto, await Users.GetAsync(dto.Id));
  }

  [Fact]
  public async Task Save_BlankName_StaysOpenAndCreatesNothing()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-save").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".profile-name .mud-input-error")));
    Assert.False(reference.Result!.IsCompleted);
    Assert.Single(await Users.ListAsync());
  }

  [Fact]
  public async Task Cancel_ClosesWithCanceledResultAndCreatesNothing()
  {
    var (provider, reference) = await ShowDialogAsync(CreateMode());
    provider.WaitForElement(".profile-name input").Input("Anna");
    provider.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(result!.Canceled);
    Assert.Single(await Users.ListAsync());
  }
}
