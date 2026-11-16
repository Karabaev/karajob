using Karajob.Domain.Normalization;

namespace Karajob.Domain.Catalogs;

public class Currency : CatalogEntry
{
  public string Code { get; private set; }

  public Currency(string code, string name)
    : base(name)
  {
    Code = Normalize.CurrencyCode(code);
  }
}
