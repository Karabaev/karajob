using System.Runtime.CompilerServices;

namespace Karajob.Domain.Common;

public static class UtcGuard
{
  public static DateTime Ensure(DateTime value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    if (value.Kind != DateTimeKind.Utc) throw new ArgumentException($"Expected a UTC DateTime, got {value.Kind}.", paramName);
    return value;
  }

  public static DateTime? Ensure(DateTime? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    return value is null ? null : Ensure(value.Value, paramName);
  }
}
