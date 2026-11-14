using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Domain.Catalogs;
using Karajob.Domain.Normalization;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Catalogs;

public class CatalogService(IKarajobDbContextFactory dbContextFactory)
{
  public async Task<IReadOnlyList<CatalogListItem>> ListAsync(CatalogKind kind, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var entries = await Entries(db, kind).AsNoTracking().ToListAsync(cancellationToken);
    return entries
      .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
      .Select(ToItem)
      .ToList();
  }

  public async Task<CatalogListItem> CreateAsync(CatalogKind kind, CatalogInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var errors = new Dictionary<string, string>();
    await ValidateNameAsync(db, kind, input.Name, Guid.Empty, errors, cancellationToken);

    string? code = null;
    if (kind == CatalogKind.Language)
    {
      code = await ValidateCodeAsync(db, input.Code, errors, cancellationToken);
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    CatalogEntry entry;
    switch (kind)
    {
      case CatalogKind.Role:
        var role = new Role(input.Name);
        db.Roles.Add(role);
        entry = role;
        break;
      case CatalogKind.Location:
        var location = new Location(input.Name);
        db.Locations.Add(location);
        entry = location;
        break;
      case CatalogKind.Language:
        var language = new Language(code!, input.Name);
        db.Languages.Add(language);
        entry = language;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    await db.SaveChangesAsync(cancellationToken);
    return ToItem(entry);
  }

  public async Task<CatalogListItem> RenameAsync(CatalogKind kind, Guid id, string name, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var entry = await FindAsync(db, kind, id, cancellationToken);

    var errors = new Dictionary<string, string>();
    await ValidateNameAsync(db, kind, name, id, errors, cancellationToken);
    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    entry.Rename(name);
    await db.SaveChangesAsync(cancellationToken);
    return ToItem(entry);
  }

  public async Task DeleteAsync(CatalogKind kind, Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var entry = await FindAsync(db, kind, id, cancellationToken);

    if (await IsReferencedAsync(db, kind, id, cancellationToken))
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = "Запись используется в гипотезах, вакансиях или CV — удалить нельзя",
      });
    }

    switch (entry)
    {
      case Role role:
        db.Roles.Remove(role);
        break;
      case Location location:
        db.Locations.Remove(location);
        break;
      case Language language:
        db.Languages.Remove(language);
        break;
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  private static IQueryable<CatalogEntry> Entries(IKarajobDbContext db, CatalogKind kind)
  {
    return kind switch
    {
      CatalogKind.Role => db.Roles,
      CatalogKind.Location => db.Locations,
      CatalogKind.Language => db.Languages,
      _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
  }

  private static async Task<CatalogEntry> FindAsync(IKarajobDbContext db, CatalogKind kind, Guid id, CancellationToken cancellationToken)
  {
    return await Entries(db, kind).FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
      ?? throw new KeyNotFoundException($"{kind} {id} was not found.");
  }

  private static async Task ValidateNameAsync(
    IKarajobDbContext db,
    CatalogKind kind,
    string name,
    Guid excludedId,
    Dictionary<string, string> errors,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      errors[nameof(CatalogInput.Name)] = "Укажите название";
      return;
    }

    var normalized = Normalize.Alias(name);
    if (await Entries(db, kind).AnyAsync(e => e.Id != excludedId && e.NormalizedName == normalized, cancellationToken))
    {
      errors[nameof(CatalogInput.Name)] = "Такая запись уже есть";
    }
  }

  private static async Task<string?> ValidateCodeAsync(
    IKarajobDbContext db,
    string? code,
    Dictionary<string, string> errors,
    CancellationToken cancellationToken)
  {
    string normalized;
    try
    {
      normalized = Normalize.LanguageCode(code!);
    }
    catch (ArgumentException)
    {
      errors[nameof(CatalogInput.Code)] = "Код — 2–3 латинские буквы";
      return null;
    }

    if (await db.Languages.AnyAsync(l => l.Code == normalized, cancellationToken))
    {
      errors[nameof(CatalogInput.Code)] = "Такой код уже есть";
    }

    return normalized;
  }

  private static async Task<bool> IsReferencedAsync(IKarajobDbContext db, CatalogKind kind, Guid id, CancellationToken cancellationToken)
  {
    switch (kind)
    {
      case CatalogKind.Role:
        return await db.Hypotheses.IgnoreQueryFilters().AnyAsync(h => h.RoleId == id, cancellationToken)
          || await db.Vacancies.IgnoreQueryFilters().AnyAsync(v => v.RoleId == id, cancellationToken);
      case CatalogKind.Location:
        return await db.HypothesisLocations.IgnoreQueryFilters().AnyAsync(l => l.LocationId == id, cancellationToken)
          || await db.Vacancies.IgnoreQueryFilters().AnyAsync(v => v.LocationId == id, cancellationToken);
      case CatalogKind.Language:
        return await db.Hypotheses.IgnoreQueryFilters().AnyAsync(h => h.LanguageId == id, cancellationToken)
          || await db.Vacancies.IgnoreQueryFilters().AnyAsync(v => v.LanguageId == id, cancellationToken)
          || await db.CvDocuments.IgnoreQueryFilters().AnyAsync(d => d.LanguageId == id, cancellationToken);
      default:
        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }
  }

  private static CatalogListItem ToItem(CatalogEntry entry)
  {
    return new CatalogListItem(entry.Id, entry.Name, (entry as Language)?.Code);
  }
}
