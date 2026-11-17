using Karajob.Application.Hypotheses;
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

  public static string DecisionKind(HypothesisDecisionKind kind)
  {
    return kind switch
    {
      HypothesisDecisionKind.Continue => Localized.Get<HypothesisStrings>("Decision.Continue"),
      HypothesisDecisionKind.Expand => Localized.Get<HypothesisStrings>("Decision.Expand"),
      HypothesisDecisionKind.Disable => Localized.Get<HypothesisStrings>("Decision.Disable"),
      _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
  }

  public static Color DecisionKindColor(HypothesisDecisionKind kind)
  {
    return kind switch
    {
      HypothesisDecisionKind.Continue => Color.Success,
      HypothesisDecisionKind.Expand => Color.Info,
      HypothesisDecisionKind.Disable => Color.Error,
      _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
  }

  public static string Hint(DecisionHint hint)
  {
    return hint switch
    {
      DecisionHint.NotEnoughData => Localized.Get<HypothesisStrings>("Hint.NotEnoughData"),
      DecisionHint.Expand => Localized.Get<HypothesisStrings>("Hint.Expand"),
      DecisionHint.Continue => Localized.Get<HypothesisStrings>("Hint.Continue"),
      DecisionHint.ReviseCv => Localized.Get<HypothesisStrings>("Hint.ReviseCv"),
      DecisionHint.Disable => Localized.Get<HypothesisStrings>("Hint.Disable"),
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

  public static string Summary(HypothesisListItem item)
  {
    throw new NotImplementedException();
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
