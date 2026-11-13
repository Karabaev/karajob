namespace Karajob.Application.Catalogs;

public record CatalogListItem(Guid Id, string Name, string? Code);

public record CatalogInput(string Name, string? Code = null);
