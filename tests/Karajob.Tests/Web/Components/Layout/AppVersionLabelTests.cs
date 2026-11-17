using Bunit;
using Karajob.Web.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Layout;

public class AppVersionLabelTests : IAsyncLifetime
{
  private const string Version = "a37224f";

  private readonly BunitContext _context = new();

  [Fact]
  public void Render_ShowsVersionText()
  {
    var cut = RenderLabel();

    Assert.Equal(Version, cut.Find(".app-version").TextContent.Trim());
  }

  [Fact]
  public void Click_WritesVersionToClipboard()
  {
    _context.JSInterop.SetupVoid("navigator.clipboard.writeText", Version);
    var cut = RenderLabel();

    cut.Find(".app-version").Click();

    cut.WaitForAssertion(() =>
    {
      var call = _context.JSInterop.VerifyInvoke("navigator.clipboard.writeText");
      Assert.Equal(Version, Assert.Single(call.Arguments));
    });
  }

  [Fact]
  public void Click_ClipboardSucceeds_ShowsSuccessSnackbar()
  {
    _context.JSInterop.SetupVoid("navigator.clipboard.writeText", Version).SetVoidResult();
    var cut = RenderLabel();

    cut.Find(".app-version").Click();

    cut.WaitForAssertion(() =>
    {
      var snackbar = Assert.Single(_context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
      Assert.Equal(Severity.Success, snackbar.Severity);
      Assert.Equal("Version copied", snackbar.Message?.ToString());
    });
  }

  [Fact]
  public void Click_ClipboardUnavailable_ShowsWarningSnackbar()
  {
    _context.JSInterop.SetupVoid("navigator.clipboard.writeText", Version).SetException(new JSException("not allowed"));
    var cut = RenderLabel();

    cut.Find(".app-version").Click();

    cut.WaitForAssertion(() =>
    {
      var snackbar = Assert.Single(_context.Services.GetRequiredService<ISnackbar>().ShownSnackbars);
      Assert.Equal(Severity.Warning, snackbar.Severity);
      Assert.Equal("Could not copy the version", snackbar.Message?.ToString());
    });
  }

  private IRenderedComponent<AppVersionLabel> RenderLabel()
  {
    return _context.Render<AppVersionLabel>(p => p.Add(l => l.Version, Version));
  }

  public Task InitializeAsync()
  {
    return Task.CompletedTask;
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    await _context.DisposeAsync();
  }

  public AppVersionLabelTests()
  {
    _context.Services.AddMudServices();
    _context.Services.AddLocalization();
  }
}
