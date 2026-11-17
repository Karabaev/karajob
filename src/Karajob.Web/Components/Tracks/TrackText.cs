using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Web.Resources;
using MudBlazor;

namespace Karajob.Web.Components.Tracks;

public static class TrackText
{
  public static string Status(TrackStatus status)
  {
    return status switch
    {
      TrackStatus.Draft => Localized.Get<TrackStrings>("Status.Draft"),
      TrackStatus.Active => Localized.Get<TrackStrings>("Status.Active"),
      TrackStatus.Expanded => Localized.Get<TrackStrings>("Status.Expanded"),
      TrackStatus.Paused => Localized.Get<TrackStrings>("Status.Paused"),
      TrackStatus.Disabled => Localized.Get<TrackStrings>("Status.Disabled"),
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
  }

  public static Color StatusColor(TrackStatus status)
  {
    return status switch
    {
      TrackStatus.Draft => Color.Default,
      TrackStatus.Active => Color.Success,
      TrackStatus.Expanded => Color.Info,
      TrackStatus.Paused => Color.Warning,
      TrackStatus.Disabled => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
  }

  public static string DecisionKind(TrackDecisionKind kind)
  {
    return kind switch
    {
      TrackDecisionKind.Continue => Localized.Get<TrackStrings>("Decision.Continue"),
      TrackDecisionKind.Expand => Localized.Get<TrackStrings>("Decision.Expand"),
      TrackDecisionKind.Disable => Localized.Get<TrackStrings>("Decision.Disable"),
      _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
  }

  public static Color DecisionKindColor(TrackDecisionKind kind)
  {
    return kind switch
    {
      TrackDecisionKind.Continue => Color.Success,
      TrackDecisionKind.Expand => Color.Info,
      TrackDecisionKind.Disable => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
  }

  public static string Hint(DecisionHint hint)
  {
    return hint switch
    {
      DecisionHint.NotEnoughData => Localized.Get<TrackStrings>("Hint.NotEnoughData"),
      DecisionHint.Expand => Localized.Get<TrackStrings>("Hint.Expand"),
      DecisionHint.Continue => Localized.Get<TrackStrings>("Hint.Continue"),
      DecisionHint.ReviseCv => Localized.Get<TrackStrings>("Hint.ReviseCv"),
      DecisionHint.Disable => Localized.Get<TrackStrings>("Hint.Disable"),
      _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, null)
    };
  }

  public static Color HintColor(DecisionHint hint)
  {
    return hint switch
    {
      DecisionHint.NotEnoughData => Color.Default,
      DecisionHint.Expand => Color.Info,
      DecisionHint.Continue => Color.Success,
      DecisionHint.ReviseCv => Color.Warning,
      DecisionHint.Disable => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, null)
    };
  }

  public static string Summary(TrackListItem item)
  {
    var parts = new List<string> { item.LanguageCode.ToUpperInvariant() };
    if (item.PlatformNames.Count > 0)
    {
      parts.Add(string.Join(", ", item.PlatformNames));
    }

    var format = Format(item.WorkFormat);
    if (format.Length > 0)
    {
      parts.Add(format);
    }

    return string.Join(" · ", parts);
  }

  public static string Format(WorkFormat format)
  {
    var labels = new List<string>();
    if (format.HasFlag(WorkFormat.Remote))
    {
      labels.Add(Localized.Get<TrackStrings>("WorkFormat.Remote"));
    }

    if (format.HasFlag(WorkFormat.Hybrid))
    {
      labels.Add(Localized.Get<TrackStrings>("WorkFormat.Hybrid"));
    }

    if (format.HasFlag(WorkFormat.OnSite))
    {
      labels.Add(Localized.Get<TrackStrings>("WorkFormat.OnSite"));
    }

    if (format.HasFlag(WorkFormat.Relocation))
    {
      labels.Add(Localized.Get<TrackStrings>("WorkFormat.Relocation"));
    }

    return string.Join(", ", labels);
  }
}
