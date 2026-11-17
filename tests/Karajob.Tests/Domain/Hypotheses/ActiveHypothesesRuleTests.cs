using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Domain.Hypotheses;

public class ActiveHypothesesRuleTests
{
  [Theory]
  [InlineData(0, ActiveHypothesesStatus.TooFew)]
  [InlineData(1, ActiveHypothesesStatus.TooFew)]
  [InlineData(2, ActiveHypothesesStatus.Ok)]
  [InlineData(3, ActiveHypothesesStatus.Ok)]
  [InlineData(4, ActiveHypothesesStatus.TooMany)]
  public void Check_ActiveCount_ReturnsStatus(int activeCount, ActiveHypothesesStatus expected)
  {
    Assert.Equal(expected, ActiveHypothesesRule.Check(activeCount));
  }
}
