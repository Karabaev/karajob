using Karajob.Domain.Hypotheses;
using Karajob.Web.Resources;
using MudBlazor;

namespace Karajob.Web.Components.Hypotheses;

public static class HypothesisText
{
  public static string Status(HypothesisStatus status)
  {
    return status switch
    {
      HypothesisStatus.Draft => Localized.Get<HypothesisStrings>("Status.Draft"),
      HypothesisStatus.Active => Localized.Get<HypothesisStrings>("Status.Active"),
      HypothesisStatus.Expanded => Localized.Get<HypothesisStrings>("Status.Expanded"),
      HypothesisStatus.Paused => Localized.Get<HypothesisStrings>("Status.Paused"),
      HypothesisStatus.Disabled => Localized.Get<HypothesisStrings>("Status.Disabled"),
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
      labels.Add(Localized.Get<HypothesisStrings>("WorkFormat.Remote"));
    }

    if (format.HasFlag(WorkFormat.Hybrid))
    {
      labels.Add(Localized.Get<HypothesisStrings>("WorkFormat.Hybrid"));
    }

    if (format.HasFlag(WorkFormat.OnSite))
    {
      labels.Add(Localized.Get<HypothesisStrings>("WorkFormat.OnSite"));
    }

    if (format.HasFlag(WorkFormat.Relocation))
    {
      labels.Add(Localized.Get<HypothesisStrings>("WorkFormat.Relocation"));
    }

    return string.Join(", ", labels);
  }
}
