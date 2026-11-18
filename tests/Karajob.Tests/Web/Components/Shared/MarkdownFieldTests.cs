using Bunit;
using Karajob.Web.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Shared;

public class MarkdownFieldTests : IAsyncLifetime
{
  private BunitContext Context { get; } = new();

  [Fact]
  public void Render_Default_HasTextareaAndPreview()
  {
    var cut = Context.Render<MarkdownField>();

    Assert.NotNull(cut.Find("textarea"));
    Assert.NotNull(cut.Find(".markdown-field-preview.markdown-body"));
  }

  [Fact]
  public void Render_ValueWithMarkdown_ShowsRenderedHtmlInPreview()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, "## Hi\n\n- a"));

    var preview = cut.Find(".markdown-field-preview");
    Assert.NotNull(preview.QuerySelector("h2"));
    Assert.NotNull(preview.QuerySelector("li"));
  }

  [Fact]
  public void Render_ValueWithMarkdown_KeepsRawTextInTextarea()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, "## Hi\n\n- a"));

    Assert.Equal("## Hi\n\n- a", cut.Find("textarea").TextContent.Replace("\r\n", "\n").TrimStart('\n'));
  }

  [Fact]
  public void Render_NullValue_HasEmptyPreview()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, (string?)null));

    Assert.Equal("", cut.Find(".markdown-field-preview").TextContent.Trim());
  }

  [Fact]
  public void Input_TextTyped_UpdatesPreview()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, ""));

    cut.Find("textarea").Input("# Title");

    cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".markdown-field-preview").QuerySelector("h1")));
  }

  [Fact]
  public void Input_TextTyped_FiresValueChangedWithRawText()
  {
    string? received = null;
    var cut = Context.Render<MarkdownField>(p => p
      .Add(c => c.Value, "")
      .Add(c => c.ValueChanged, (string? v) => received = v));

    cut.Find("textarea").Input("## Duties\n\n- one");

    cut.WaitForAssertion(() => Assert.Equal("## Duties\n\n- one", received));
  }

  [Fact]
  public void Render_ScriptInValue_IsEscapedInPreview()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, "<script>alert(1)</script>"));

    var preview = cut.Find(".markdown-field-preview");
    Assert.Null(preview.QuerySelector("script"));
    Assert.Contains("&lt;script&gt;", preview.InnerHtml);
  }

  [Fact]
  public void Input_ScriptTyped_IsEscapedInPreview()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Value, ""));

    cut.Find("textarea").Input("<script>alert(1)</script>");

    cut.WaitForAssertion(() =>
    {
      var preview = cut.Find(".markdown-field-preview");
      Assert.Null(preview.QuerySelector("script"));
      Assert.Contains("&lt;script&gt;", preview.InnerHtml);
    });
  }

  [Fact]
  public void Render_Default_EditorPaneVisibleAndPreviewPaneHidden()
  {
    var cut = Context.Render<MarkdownField>();

    Assert.DoesNotContain("markdown-field-pane-hidden", cut.Find(".markdown-field-editor").ClassList);
    Assert.Contains("markdown-field-pane-hidden", cut.Find(".markdown-field-preview").ClassList);
  }

  [Fact]
  public void ClickPreviewToggle_Default_HidesEditorAndShowsPreview()
  {
    var cut = Context.Render<MarkdownField>();

    cut.Find(".markdown-field-tab-preview").Click();

    Assert.Contains("markdown-field-pane-hidden", cut.Find(".markdown-field-editor").ClassList);
    Assert.DoesNotContain("markdown-field-pane-hidden", cut.Find(".markdown-field-preview").ClassList);
  }

  [Fact]
  public void ClickEditorToggle_AfterPreviewToggle_SwapsBack()
  {
    var cut = Context.Render<MarkdownField>();
    cut.Find(".markdown-field-tab-preview").Click();

    cut.Find(".markdown-field-tab-editor").Click();

    Assert.DoesNotContain("markdown-field-pane-hidden", cut.Find(".markdown-field-editor").ClassList);
    Assert.Contains("markdown-field-pane-hidden", cut.Find(".markdown-field-preview").ClassList);
  }

  [Fact]
  public void Render_Class_IsAppliedToRoot()
  {
    var cut = Context.Render<MarkdownField>(p => p.Add(c => c.Class, "my-field"));

    Assert.NotNull(cut.Find(".my-field"));
  }

  public Task InitializeAsync() => Task.CompletedTask;

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await Context.DisposeAsync();
  }

  public MarkdownFieldTests()
  {
    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    Context.Services.AddMudServices();
    Context.Services.AddLocalization();
  }
}
