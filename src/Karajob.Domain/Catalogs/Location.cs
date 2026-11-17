namespace Karajob.Domain.Catalogs;

public class Location(string name) : CatalogEntry(name)
{
  public string? TimeZoneId { get; private set; }

  public void SetTimeZone(string? timeZoneId)
  {
    throw new NotImplementedException();
  }
}
