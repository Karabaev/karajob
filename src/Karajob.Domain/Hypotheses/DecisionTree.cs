namespace Karajob.Domain.Hypotheses;

public record DecisionTreeInput(
  int MarketVacancies,
  int MarketThreshold,
  int PositiveApplications,
  int MaturedApplications,
  int ConversionThresholdPermille,
  int MinSampleSize,
  bool WellBeingLow);

public static class DecisionTree
{
  public static DecisionHint Suggest(DecisionTreeInput input)
  {
    if (input.WellBeingLow) return DecisionHint.Disable;
    if (input.MarketVacancies < input.MarketThreshold) return DecisionHint.Expand;
    if (input.MaturedApplications < input.MinSampleSize) return DecisionHint.NotEnoughData;

    return input.PositiveApplications * 1000L >= input.ConversionThresholdPermille * (long)input.MaturedApplications
      ? DecisionHint.Continue
      : DecisionHint.ReviseCv;
  }
}
