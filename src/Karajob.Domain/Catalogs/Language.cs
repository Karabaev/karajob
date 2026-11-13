using Karajob.Domain.Normalization;

namespace Karajob.Domain.Catalogs;

public class Language : CatalogEntry
{
  public string Code { get; private set; }

  public Language(string code, string name)
    : base(name)
  {
    Code = Normalize.LanguageCode(code);
  }
}
