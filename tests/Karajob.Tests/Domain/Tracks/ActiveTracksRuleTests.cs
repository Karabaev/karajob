using Karajob.Domain.Tracks;

namespace Karajob.Tests.Domain.Tracks;

public class ActiveTracksRuleTests
{
  [Theory]
  [InlineData(0, ActiveTracksStatus.TooFew)]
  [InlineData(1, ActiveTracksStatus.TooFew)]
  [InlineData(2, ActiveTracksStatus.Ok)]
  [InlineData(3, ActiveTracksStatus.Ok)]
  [InlineData(4, ActiveTracksStatus.TooMany)]
  public void Check_ActiveCount_ReturnsStatus(int activeCount, ActiveTracksStatus expected)
  {
    Assert.Equal(expected, ActiveTracksRule.Check(activeCount));
  }
}
