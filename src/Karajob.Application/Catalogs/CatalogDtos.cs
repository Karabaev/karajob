namespace Karajob.Application.Catalogs;

public record CatalogListItem(Guid Id, string Name, string? Code, string? TimeZoneId = null);

public record CatalogInput(string Name, string? Code = null);
