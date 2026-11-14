using Bunit;
using Karajob.Application.Users;
using Karajob.Web.Components.Profiles;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Profiles;

public class ProfileAvatarTests : ProfileWebFixture
{
  [Theory]
  [InlineData("Anna", "A")]
  [InlineData("anna", "A")]
  [InlineData("анна", "А")]
  public void Render_ShowsFirstLetterOfNameUpperCased(string name, string letter)
  {
    var cut = RenderAvatar(new UserDto(Guid.NewGuid(), name, "#26a69a"));

    Assert.Equal(letter, cut.Find(".mud-avatar").TextContent.Trim());
  }

  [Fact]
  public void Render_UsesProfileColorAsBackground()
  {
    var cut = RenderAvatar(new UserDto(Guid.NewGuid(), "Anna", "#26a69a"));

    var styled = cut.FindAll("[style]").Select(e => e.GetAttribute("style")!);
    Assert.Contains(styled, s => s.Contains("#26a69a", StringComparison.OrdinalIgnoreCase) && s.Contains("background", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Render_DifferentColor_UsesThatColor()
  {
    var cut = RenderAvatar(new UserDto(Guid.NewGuid(), "Anna", "#ef5350"));

    Assert.Contains("#ef5350", cut.Markup, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("#26a69a", cut.Markup, StringComparison.OrdinalIgnoreCase);
  }

  private IRenderedComponent<ProfileAvatar> RenderAvatar(UserDto profile)
  {
    return Context.Render<ProfileAvatar>(p => p.Add(a => a.Profile, profile).Add(a => a.Size, Size.Large));
  }
}
