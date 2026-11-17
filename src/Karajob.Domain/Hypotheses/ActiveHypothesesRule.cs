namespace Karajob.Domain.Hypotheses;

public static class ActiveHypothesesRule
{
  public const int MinActive = 2;
  public const int MaxActive = 3;

  public static bool IsActive(HypothesisStatus status)
  {
    return status is HypothesisStatus.Active or HypothesisStatus.Expanded;
  }

  public static ActiveHypothesesStatus Check(int activeCount)
  {
    if (activeCount < MinActive) return ActiveHypothesesStatus.TooFew;
    if (activeCount > MaxActive) return ActiveHypothesesStatus.TooMany;

    return ActiveHypothesesStatus.Ok;
  }
}
