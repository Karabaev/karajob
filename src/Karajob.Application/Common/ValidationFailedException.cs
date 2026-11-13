namespace Karajob.Application.Common;

public class ValidationFailedException(IReadOnlyDictionary<string, string> errors)
  : Exception(string.Join(" ", errors.Values))
{
  public IReadOnlyDictionary<string, string> Errors { get; } = errors;
}
