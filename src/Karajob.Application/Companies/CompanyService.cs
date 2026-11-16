using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Companies;
using Karajob.Domain.Normalization;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Companies;

public class CompanyService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, IStringLocalizer<CompanyMessages> localizer)
{
  // Company pages live at /companies/{slug}; these slugs collide with fixed routes there.
  private static readonly HashSet<string> ReservedSlugs = ["new"];

  public async Task<IReadOnlyList<CompanyListItem>> SearchAsync(string? query, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    // SQLite LIKE does not fold case for Cyrillic, so the filter runs in memory.
    var companies = await db.Companies.AsNoTracking().ToListAsync(cancellationToken);
    var term = query?.Trim();
    if (!string.IsNullOrEmpty(term))
    {
      companies = companies.Where(c => Matches(c, term)).ToList();
    }

    var blacklisted = (await db.UserCompanyNotes.AsNoTracking()
        .Where(n => n.IsBlacklisted)
        .Select(n => n.CompanyId)
        .ToListAsync(cancellationToken))
      .ToHashSet();

    return companies
      .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
      .Select(c => new CompanyListItem(
        c.Id,
        c.Slug,
        c.Name,
        IconUrl(c),
        SortedDomains(c),
        blacklisted.Contains(c.Id)))
      .ToList();
  }

  public async Task<CompanyDetails?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    string normalized;
    try
    {
      normalized = Normalize.Slug(slug);
    }
    catch (ArgumentException)
    {
      return null;
    }

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == normalized, cancellationToken);
    return company is null ? null : await BuildDetailsAsync(db, company, cancellationToken);
  }

  public async Task<CompanyDetails> CreateAsync(CompanyInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalized = await ValidateAsync(db, input, Guid.Empty, cancellationToken);

    var company = new Company(normalized.Slug, normalized.Name)
    {
      Website = normalized.Website,
      Description = normalized.Description,
    };
    SyncValues(company, normalized);

    db.Companies.Add(company);
    await db.SaveChangesAsync(cancellationToken);
    return await BuildDetailsAsync(db, company, cancellationToken);
  }

  public async Task<CompanyDetails> UpdateAsync(Guid companyId, CompanyInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var company = await FindCompanyAsync(db, companyId, cancellationToken);
    var normalized = await ValidateAsync(db, input, companyId, cancellationToken);

    company.ChangeSlug(normalized.Slug);
    company.Name = normalized.Name;
    company.Website = normalized.Website;
    company.Description = normalized.Description;
    SyncValues(company, normalized);

    await db.SaveChangesAsync(cancellationToken);
    return await BuildDetailsAsync(db, company, cancellationToken);
  }

  public async Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var company = await FindCompanyAsync(db, companyId, cancellationToken);

    if (await db.Projects.AnyAsync(p => p.CompanyId == companyId, cancellationToken))
    {
      throw WholeEntityError(localizer["Error.HasProjects"]);
    }

    if (await db.Vacancies.IgnoreQueryFilters().AnyAsync(v => v.CompanyId == companyId, cancellationToken))
    {
      throw WholeEntityError(localizer["Error.HasVacancies"]);
    }

    var notes = await db.UserCompanyNotes.IgnoreQueryFilters().Where(n => n.CompanyId == companyId).ToListAsync(cancellationToken);
    db.UserCompanyNotes.RemoveRange(notes);
    db.Companies.Remove(company);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<ProjectItem> AddProjectAsync(Guid companyId, ProjectInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindCompanyAsync(db, companyId, cancellationToken);
    var normalized = await ValidateProjectAsync(db, input, cancellationToken);

    var project = new Project(companyId, normalized.Name, input.Status)
    {
      Genre = normalized.Genre,
      EngineId = input.EngineId,
      Url = normalized.Url,
    };
    db.Projects.Add(project);
    await db.SaveChangesAsync(cancellationToken);
    return ToItem(project, await FindEngineNameAsync(db, project.EngineId, cancellationToken));
  }

  public async Task<ProjectItem> UpdateProjectAsync(Guid projectId, ProjectInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var project = await FindProjectAsync(db, projectId, cancellationToken);
    var normalized = await ValidateProjectAsync(db, input, cancellationToken);

    project.Name = normalized.Name;
    project.Genre = normalized.Genre;
    project.EngineId = input.EngineId;
    project.Status = input.Status;
    project.Url = normalized.Url;
    await db.SaveChangesAsync(cancellationToken);
    return ToItem(project, await FindEngineNameAsync(db, project.EngineId, cancellationToken));
  }

  public async Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var project = await FindProjectAsync(db, projectId, cancellationToken);
    db.Projects.Remove(project);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task SaveNoteAsync(Guid companyId, string? note, bool isBlacklisted, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    await FindCompanyAsync(db, companyId, cancellationToken);

    var entry = await db.UserCompanyNotes.FirstOrDefaultAsync(n => n.CompanyId == companyId, cancellationToken);
    if (entry is null)
    {
      entry = new UserCompanyNote(currentUser.UserId, companyId);
      db.UserCompanyNotes.Add(entry);
    }

    entry.Note = BlankToNull(note);
    entry.IsBlacklisted = isBlacklisted;
    await db.SaveChangesAsync(cancellationToken);
  }

  private static async Task<Company> FindCompanyAsync(IKarajobDbContext db, Guid companyId, CancellationToken cancellationToken)
  {
    return await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
      ?? throw new KeyNotFoundException($"Company {companyId} was not found.");
  }

  private static async Task<Project> FindProjectAsync(IKarajobDbContext db, Guid projectId, CancellationToken cancellationToken)
  {
    return await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
      ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
  }

  private async Task<CompanyDetails> BuildDetailsAsync(IKarajobDbContext db, Company company, CancellationToken cancellationToken)
  {
    var projects = await db.Projects.AsNoTracking().Where(p => p.CompanyId == company.Id).ToListAsync(cancellationToken);
    var engineIds = projects.Where(p => p.EngineId is not null).Select(p => p.EngineId!.Value).Distinct().ToList();
    var engineNames = await db.Engines.AsNoTracking()
      .Where(e => engineIds.Contains(e.Id))
      .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
    var note = await db.UserCompanyNotes.AsNoTracking().FirstOrDefaultAsync(n => n.CompanyId == company.Id, cancellationToken);

    return new CompanyDetails(
      company.Id,
      company.Slug,
      company.Name,
      company.Website,
      company.Description,
      IconUrl(company),
      SortedDomains(company),
      company.Aliases.Select(a => a.Value).Order(StringComparer.Ordinal).ToList(),
      projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(p => ToItem(p, p.EngineId is { } id ? engineNames[id] : null)).ToList(),
      note?.Note,
      note?.IsBlacklisted ?? false);
  }

  private async Task<NormalizedCompany> ValidateAsync(
    IKarajobDbContext db,
    CompanyInput input,
    Guid excludedCompanyId,
    CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    var slug = Try(() => Normalize.Slug(input.Slug), errors, nameof(CompanyInput.Slug), localizer["Error.SlugInvalid"]);
    var name = string.IsNullOrWhiteSpace(input.Name) ? null : input.Name.Trim();
    if (name is null)
    {
      errors[nameof(CompanyInput.Name)] = localizer["Error.NameRequired"];
    }

    var website = BlankToNull(input.Website);
    if (website is not null)
    {
      website = Try(() => Normalize.WebUrl(website), errors, nameof(CompanyInput.Website), localizer["Error.WebsiteInvalid"]);
    }

    var domains = new List<string>();
    var invalidDomains = new List<string>();
    foreach (var raw in input.Domains.Where(d => !string.IsNullOrWhiteSpace(d)))
    {
      try
      {
        domains.Add(Normalize.Domain(raw));
      }
      catch (ArgumentException)
      {
        invalidDomains.Add(raw.Trim());
      }
    }

    if (invalidDomains.Count > 0)
    {
      errors[nameof(CompanyInput.Domains)] = localizer["Error.DomainInvalid", string.Join(", ", invalidDomains)];
    }

    var aliases = input.Aliases
      .Where(a => !string.IsNullOrWhiteSpace(a))
      .Select(a => Normalize.Alias(a))
      .Distinct()
      .ToList();
    domains = domains.Distinct().ToList();

    if (slug is not null && ReservedSlugs.Contains(slug))
    {
      errors[nameof(CompanyInput.Slug)] = localizer["Error.SlugReserved", slug];
      slug = null;
    }

    if (slug is not null)
    {
      var owner = await db.Companies
        .Where(c => c.Id != excludedCompanyId && c.Slug == slug)
        .Select(c => c.Name)
        .FirstOrDefaultAsync(cancellationToken);
      if (owner is not null)
      {
        errors[nameof(CompanyInput.Slug)] = localizer["Error.SlugTaken", slug, owner];
      }
    }

    if (domains.Count > 0 && !errors.ContainsKey(nameof(CompanyInput.Domains)))
    {
      var taken = await (
          from d in db.CompanyDomains
          join c in db.Companies on d.CompanyId equals c.Id
          where c.Id != excludedCompanyId && domains.Contains(d.Value)
          orderby d.Value
          select new { d.Value, c.Name })
        .ToListAsync(cancellationToken);
      if (taken.Count > 0)
      {
        errors[nameof(CompanyInput.Domains)] = string.Join("; ", taken.Select(t => localizer["Error.DomainTaken", t.Value, t.Name]));
      }
    }

    if (aliases.Count > 0)
    {
      var taken = await (
          from a in db.CompanyAliases
          join c in db.Companies on a.CompanyId equals c.Id
          where c.Id != excludedCompanyId && aliases.Contains(a.Value)
          orderby a.Value
          select new { a.Value, c.Name })
        .ToListAsync(cancellationToken);
      if (taken.Count > 0)
      {
        errors[nameof(CompanyInput.Aliases)] = string.Join("; ", taken.Select(t => localizer["Error.AliasTaken", t.Value, t.Name]));
      }
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedCompany(slug!, name!, website, BlankToNull(input.Description), domains, aliases);
  }

  private async Task<NormalizedProject> ValidateProjectAsync(IKarajobDbContext db, ProjectInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    var name = string.IsNullOrWhiteSpace(input.Name) ? null : input.Name.Trim();
    if (name is null)
    {
      errors[nameof(ProjectInput.Name)] = localizer["Error.NameRequired"];
    }

    var url = BlankToNull(input.Url);
    if (url is not null)
    {
      url = Try(() => Normalize.WebUrl(url), errors, nameof(ProjectInput.Url), localizer["Error.UrlInvalid"]);
    }

    if (input.EngineId is { } engineId && !await db.Engines.AnyAsync(e => e.Id == engineId, cancellationToken))
    {
      errors[nameof(ProjectInput.EngineId)] = localizer["Error.EngineNotFound"];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedProject(name!, BlankToNull(input.Genre), url);
  }

  private static void SyncValues(Company company, NormalizedCompany normalized)
  {
    foreach (var domain in company.Domains.Where(d => !normalized.Domains.Contains(d.Value)).Select(d => d.Value).ToList())
    {
      company.RemoveDomain(domain);
    }

    foreach (var alias in company.Aliases.Where(a => !normalized.Aliases.Contains(a.Value)).Select(a => a.Value).ToList())
    {
      company.RemoveAlias(alias);
    }

    foreach (var domain in normalized.Domains)
    {
      company.AddDomain(domain);
    }

    foreach (var alias in normalized.Aliases)
    {
      company.AddAlias(alias);
    }
  }

  private static string? Try(Func<string> normalize, Dictionary<string, string> errors, string key, string message)
  {
    try
    {
      return normalize();
    }
    catch (ArgumentException)
    {
      errors[key] = message;
      return null;
    }
  }

  private static bool Matches(Company company, string term)
  {
    return Contains(company.Name, term)
      || Contains(company.Slug, term)
      || company.Domains.Any(d => Contains(d.Value, term))
      || company.Aliases.Any(a => Contains(a.Value, term));
  }

  private static bool Contains(string value, string term)
  {
    return value.Contains(term, StringComparison.OrdinalIgnoreCase);
  }

  private static List<string> SortedDomains(Company company)
  {
    return company.Domains.Select(d => d.Value).Order(StringComparer.Ordinal).ToList();
  }

  private static string? IconUrl(Company company)
  {
    var host = company.Website is null ? SortedDomains(company).FirstOrDefault() : new Uri(company.Website).Host;
    return host is null ? null : $"https://{host}/favicon.ico";
  }

  private static async Task<string?> FindEngineNameAsync(IKarajobDbContext db, Guid? engineId, CancellationToken cancellationToken)
  {
    if (engineId is null)
    {
      return null;
    }

    return await db.Engines.AsNoTracking().Where(e => e.Id == engineId).Select(e => e.Name).SingleAsync(cancellationToken);
  }

  private static ProjectItem ToItem(Project project, string? engineName)
  {
    return new ProjectItem(project.Id, project.Name, project.Genre, project.EngineId, engineName, project.Status, project.Url);
  }

  private static ValidationFailedException WholeEntityError(string message)
  {
    return new ValidationFailedException(new Dictionary<string, string> { [string.Empty] = message });
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record NormalizedCompany(string Slug, string Name, string? Website, string? Description, List<string> Domains, List<string> Aliases);

  private record NormalizedProject(string Name, string? Genre, string? Url);
}
