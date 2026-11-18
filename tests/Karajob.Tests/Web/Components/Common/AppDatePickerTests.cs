using System.Globalization;
using Bunit;
using Karajob.Web.Components.Common;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Karajob.Tests.Web.Components.Common;

public class AppDatePickerTests : IAsyncLifetime
{
  private readonly BunitContext _context = new();
  private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

  [Fact]
  public void Render_SundayFirstCulture_StartsWeekOnMonday()
  {
    var cut = RenderPicker(p => { });

    Assert.Equal("Mon", FirstWeekdayHeader(cut));
  }

  [Fact]
  public void Render_ExplicitFirstDayOfWeek_Wins()
  {
    var cut = RenderPicker(p => p.Add(x => x.FirstDayOfWeek, DayOfWeek.Sunday));

    Assert.Equal("Sun", FirstWeekdayHeader(cut));
  }

  private IRenderedComponent<AppDatePicker> RenderPicker(Action<ComponentParameterCollectionBuilder<AppDatePicker>> configure)
  {
    return _context.Render<AppDatePicker>(p =>
    {
      p.Add(x => x.PickerVariant, PickerVariant.Static);
      configure(p);
    });
  }

  private static string FirstWeekdayHeader(IRenderedComponent<AppDatePicker> cut)
  {
    return cut.Find(".mud-day-label").TextContent.Trim();
  }

  public Task InitializeAsync()
  {
    return Task.CompletedTask;
  }

  // MudBlazor services are IAsyncDisposable only, and xUnit v2 disposes test classes synchronously.
  public async Task DisposeAsync()
  {
    CultureInfo.CurrentCulture = _originalCulture;
    await _context.DisposeAsync();
  }

  public AppDatePickerTests()
  {
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    _context.Services.AddMudServices();
    _context.Services.AddLocalization();
    _context.JSInterop.Mode = JSRuntimeMode.Loose;
  }
}
