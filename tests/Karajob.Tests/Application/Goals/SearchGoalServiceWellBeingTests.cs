using Karajob.Application.Common;
using Karajob.Application.Goals;

namespace Karajob.Tests.Application.Goals;

public class SearchGoalServiceWellBeingTests : SearchGoalServiceFixture
{
  [Fact]
  public async Task GetAsync_FreshProfile_ReturnsDefaultWellBeingThreshold()
  {
    var details = await Service.GetAsync();

    Assert.Equal(2, details.WellBeingThreshold);
  }

  [Fact]
  public async Task UpdateAsync_ValidThreshold_IsPersistedAndReturned()
  {
    var updated = await Service.UpdateAsync(Input() with { WellBeingThreshold = 4 });

    Assert.Equal(4, updated.WellBeingThreshold);
    Assert.Equal(4, (await Service.GetAsync()).WellBeingThreshold);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(5)]
  public async Task UpdateAsync_ThresholdAtBounds_IsAccepted(int threshold)
  {
    await Service.UpdateAsync(Input() with { WellBeingThreshold = threshold });

    Assert.Equal(threshold, (await Service.GetAsync()).WellBeingThreshold);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(-3)]
  public async Task UpdateAsync_ThresholdOutOfRange_FailsOnThresholdAndKeepsStored(int threshold)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(Input(weeklyApplicationTarget: 15) with { WellBeingThreshold = threshold }));

    Assert.Contains(nameof(SearchGoalInput.WellBeingThreshold), exception.Errors.Keys);
    var stored = await Service.GetAsync();
    Assert.Equal(2, stored.WellBeingThreshold);
    Assert.Equal(10, stored.WeeklyApplicationTarget);
  }

  [Fact]
  public async Task UpdateAsync_ThresholdOfOneProfile_DoesNotAffectAnother()
  {
    var other = await CreateProfileAsync();

    await Service.UpdateAsync(Input() with { WellBeingThreshold = 5 });

    Assert.Equal(2, (await ServiceFor(other).GetAsync()).WellBeingThreshold);
  }
}
