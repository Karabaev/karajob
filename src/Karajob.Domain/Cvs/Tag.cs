using System.Text.RegularExpressions;
using Karajob.Domain.Common;

namespace Karajob.Domain.Cvs;

public partial class Tag : IUserOwned
{
  public const int MaxNameLength = 50;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; private set; }
  public string? Color { get; private set; }

  public static string NormalizeName(string value)
  {
    var name = Require.NotBlank(value).ToLowerInvariant();
    if (name.Length > MaxNameLength)
    {
      throw new ArgumentException($"Tag name must not exceed {MaxNameLength} characters.", nameof(value));
    }

    return name;
  }

  public void Rename(string name)
  {
    Name = NormalizeName(name);
  }

  public void SetColor(string? color)
  {
    if (color is null)
    {
      Color = null;
      return;
    }

    var normalized = color.Trim().ToLowerInvariant();
    if (!ColorPattern().IsMatch(normalized))
    {
      throw new ArgumentException($"'{color}' is not a #rrggbb color.", nameof(color));
    }

    Color = normalized;
  }

  [GeneratedRegex("^#[0-9a-f]{6}$")]
  private static partial Regex ColorPattern();

  public Tag(Guid userId, string name)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = NormalizeName(name);
  }
}
