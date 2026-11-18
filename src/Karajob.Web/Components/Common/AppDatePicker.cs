using MudBlazor;

namespace Karajob.Web.Components.Common;

public class AppDatePicker : MudDatePicker
{
  protected override void OnInitialized()
  {
    FirstDayOfWeek ??= DayOfWeek.Monday;
    base.OnInitialized();
  }
}
