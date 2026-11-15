using Karajob.Domain.Metrics;

namespace Karajob.Tests.Domain.Metrics;

public class WilsonIntervalTests
{
  [Fact]
  public void Compute_OneOfThirty_ReturnsKnownBounds()
  {
    var interval = WilsonInterval.Compute(1, 30);

    Assert.Equal(0.006, interval.Lower, 3);
    Assert.Equal(0.167, interval.Upper, 3);
  }

  [Fact]
  public void Compute_ZeroOfThirty_LowerIsZeroAndUpperIsKnown()
  {
    var interval = WilsonInterval.Compute(0, 30);

    Assert.Equal(0.0, interval.Lower, 10);
    Assert.Equal(0.1135, interval.Upper, 3);
  }

  [Fact]
  public void Compute_ThirtyOfThirty_UpperIsOneAndLowerIsKnown()
  {
    var interval = WilsonInterval.Compute(30, 30);

    Assert.Equal(1.0, interval.Upper, 10);
    Assert.Equal(0.8865, interval.Lower, 3);
  }

  [Fact]
  public void Compute_SingleFailure_BoundsStayWithinUnitRange()
  {
    var interval = WilsonInterval.Compute(0, 1);

    Assert.InRange(interval.Lower, 0.0, 1.0);
    Assert.InRange(interval.Upper, 0.0, 1.0);
    Assert.True(interval.Lower <= interval.Upper);
  }

  [Fact]
  public void Compute_LargerSample_ProducesNarrowerInterval()
  {
    var small = WilsonInterval.Compute(1, 10);
    var large = WilsonInterval.Compute(10, 100);

    Assert.True(large.Upper - large.Lower < small.Upper - small.Lower);
  }

  [Fact]
  public void Compute_ZeroTrials_Throws()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => WilsonInterval.Compute(0, 0));
  }

  [Fact]
  public void Compute_NegativeSuccesses_Throws()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => WilsonInterval.Compute(-1, 10));
  }

  [Fact]
  public void Compute_SuccessesAboveTrials_Throws()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => WilsonInterval.Compute(11, 10));
  }
}
