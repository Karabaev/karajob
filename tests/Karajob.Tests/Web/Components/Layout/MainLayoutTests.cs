using AngleSharp.Dom;
using Bunit;
using Karajob.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Layout;

public class MainLayoutTests : BunitContext
{
  private const string BodyMarker = "body-marker-text";
  private const string DialogTitle = "Новая вакансия";

  [Fact]
  public void Render_RendersBody()
  {
    var cut = RenderLayout();

    Assert.Contains(BodyMarker, cut.Markup);
  }

  [Theory]
  [InlineData("inbox", "Inbox")]
  [InlineData("companies", "Компании")]
  [InlineData("hypotheses", "Гипотезы")]
  [InlineData("cvs", "CV")]
  public void Render_HasNavLink(string href, string text)
  {
    var cut = RenderLayout();

    var link = FindNavLink(cut, text);

    Assert.Equal(href, link.GetAttribute("href")!.Trim('/'));
  }

  [Fact]
  public void Render_HasBoardNavLinkToRoot()
  {
    var cut = RenderLayout();

    var link = FindNavLink(cut, "Доска");

    Assert.Equal(string.Empty, link.GetAttribute("href")!.Trim('/'));
  }

  [Fact]
  public void Render_HasSingleAddVacancyButtonWithText()
  {
    var cut = RenderLayout();

    var button = Assert.Single(cut.FindAll(".add-vacancy-button"));
    Assert.Contains("+ Вакансия", button.TextContent);
  }

  [Fact]
  public void Render_HasSingleAddVacancyFab()
  {
    var cut = RenderLayout();

    Assert.Single(cut.FindAll(".add-vacancy-fab"));
  }

  [Fact]
  public void Render_DoesNotShowDialogInitially()
  {
    var cut = RenderLayout();

    Assert.DoesNotContain(DialogTitle, cut.Markup);
  }

  [Fact]
  public void ClickAddVacancyButton_OpensNewVacancyDialog()
  {
    var cut = RenderLayout();

    cut.Find(".add-vacancy-button").Click();

    cut.WaitForAssertion(() => Assert.Contains(DialogTitle, cut.Markup));
  }

  [Fact]
  public void ClickAddVacancyFab_OpensNewVacancyDialog()
  {
    var cut = RenderLayout();

    cut.Find(".add-vacancy-fab").Click();

    cut.WaitForAssertion(() => Assert.Contains(DialogTitle, cut.Markup));
  }

  private static IElement FindNavLink(IRenderedComponent<MainLayout> cut, string text)
  {
    return cut.FindAll("a").First(a => a.TextContent.Trim() == text);
  }

  private IRenderedComponent<MainLayout> RenderLayout()
  {
    return Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, BodyMarker))));
  }

  public MainLayoutTests()
  {
    JSInterop.Mode = JSRuntimeMode.Loose;
    Services.AddMudServices();
  }
}
