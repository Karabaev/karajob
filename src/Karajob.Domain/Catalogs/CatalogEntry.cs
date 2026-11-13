namespace Karajob.Domain.Catalogs;

public abstract class CatalogEntry
{
  public Guid Id { get; private set; }
  public string Name { get; private set; }
  public string NormalizedName { get; private set; }

  public void Rename(string name)
  {
    throw new NotImplementedException();
  }

  protected CatalogEntry(string name)
  {
    Id = Guid.CreateVersion7();
    Name = null!;
    NormalizedName = null!;
    Rename(name);
  }
}
