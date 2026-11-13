using Karajob.Domain.Hypotheses;
using MudBlazor;

namespace Karajob.Web.Components.Hypotheses;

public static class HypothesisText
{
  public static string Status(HypothesisStatus status)
  {
    return status switch
    {
      HypothesisStatus.Draft => "Черновик",
      HypothesisStatus.Active => "Активна",
      HypothesisStatus.Expanded => "Расширена",
      HypothesisStatus.Paused => "На паузе",
      HypothesisStatus.Disabled => "Отключена",
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
  }

  public static Color StatusColor(HypothesisStatus status)
  {
    return status switch
    {
      HypothesisStatus.Draft => Color.Default,
      HypothesisStatus.Active => Color.Success,
      HypothesisStatus.Expanded => Color.Info,
      HypothesisStatus.Paused => Color.Warning,
      HypothesisStatus.Disabled => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
  }

  public static string Format(WorkFormat format)
  {
    var labels = new List<string>();
    if (format.HasFlag(WorkFormat.Remote))
    {
      labels.Add("Удалёнка");
    }

    if (format.HasFlag(WorkFormat.Hybrid))
    {
      labels.Add("Гибрид");
    }

    if (format.HasFlag(WorkFormat.OnSite))
    {
      labels.Add("Офис");
    }

    if (format.HasFlag(WorkFormat.Relocation))
    {
      labels.Add("Релокация");
    }

    return string.Join(", ", labels);
  }
}
