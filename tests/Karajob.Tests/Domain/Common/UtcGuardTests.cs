using Karajob.Domain.Common;

namespace Karajob.Tests.Domain.Common;

public class UtcGuardTests
{
  [Fact]
  public void Ensure_UtcValue_ReturnsSameValue()
  {
    var value = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    Assert.Equal(value, UtcGuard.Ensure(value));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ensure_NonUtcValue_Throws(DateTimeKind kind)
  {
    var value = new DateTime(2026, 11, 12, 10, 0, 0, kind);

    Assert.ThrowsAny<ArgumentException>(() => UtcGuard.Ensure(value));
  }

  [Fact]
  public void Ensure_NullableNull_ReturnsNull()
  {
    Assert.Null(UtcGuard.Ensure((DateTime?)null));
  }

  [Fact]
  public void Ensure_NullableUtc_ReturnsSameValue()
  {
    DateTime? value = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    Assert.Equal(value, UtcGuard.Ensure(value));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ensure_NullableNonUtc_Throws(DateTimeKind kind)
  {
    DateTime? value = new DateTime(2026, 11, 12, 10, 0, 0, kind);

    Assert.ThrowsAny<ArgumentException>(() => UtcGuard.Ensure(value));
  }
}
