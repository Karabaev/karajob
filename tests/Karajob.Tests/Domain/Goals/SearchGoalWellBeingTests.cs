using Karajob.Domain.Goals;

namespace Karajob.Tests.Domain.Goals;

public class SearchGoalWellBeingTests
{
  private static SearchGoal Create() => new(Guid.NewGuid());

  [Fact]
  public void Ctor_Valid_DefaultsWellBeingThresholdToTwo()
  {
    Assert.Equal(2, Create().WellBeingThreshold);
    Assert.Equal(2, SearchGoalDefaults.WellBeingThreshold);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(3)]
  [InlineData(5)]
  public void WellBeingThreshold_InRange_IsStored(int value)
  {
    var goal = Create();

    goal.WellBeingThreshold = value;

    Assert.Equal(value, goal.WellBeingThreshold);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(-1)]
  public void WellBeingThreshold_OutOfRange_ThrowsAndKeepsValue(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.WellBeingThreshold = value);

    Assert.Equal(2, goal.WellBeingThreshold);
  }
}
