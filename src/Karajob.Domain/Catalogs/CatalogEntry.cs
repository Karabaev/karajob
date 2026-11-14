using System.Diagnostics.CodeAnalysis;
using Karajob.Domain.Common;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Catalogs;

public abstract class CatalogEntry
{
  public Guid Id { get; private set; }
  public string Name { get; private set; }
  public string NormalizedName { get; private set; }

  [MemberNotNull(nameof(Name), nameof(NormalizedName))]
  public void Rename(string name)
  {
    var trimmed = Require.NotBlank(name);
    NormalizedName = Normalize.Alias(trimmed);
    Name = trimmed;
  }

  protected CatalogEntry(string name)
  {
    Id = Guid.CreateVersion7();
    Rename(name);
  }
}
