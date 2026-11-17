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
    throw new NotImplementedException();
  }
}
