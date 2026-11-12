using System.Runtime.CompilerServices;

namespace Karajob.Domain.Common;

public static class Require
{
  public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    if (value == Guid.Empty) throw new ArgumentException("Id must not be empty.", paramName);
    return value;
  }

  public static string NotBlank(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    return value.Trim();
  }
}
