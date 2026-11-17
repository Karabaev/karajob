using Karajob.Domain.Common;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Companies;

public class Company
{
  private readonly List<CompanyDomain> domains = [];
  private readonly List<CompanyAlias> aliases = [];
  private readonly List<CompanyLocation> locations = [];

  public Guid Id { get; private set; }
  public string Slug { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public string? Website { get; set => field = value is null ? null : Normalize.WebUrl(value); }
  public string? Description { get; set; }
  public string? IconPath { get; set; }
  public IReadOnlyCollection<CompanyDomain> Domains => domains;
  public IReadOnlyCollection<CompanyAlias> Aliases => aliases;
  public IReadOnlyCollection<CompanyLocation> Locations => locations;

  public void SetLocations(IEnumerable<Guid> locationIds)
  {
    var wanted = locationIds.Select(id => Require.NotEmpty(id, nameof(locationIds))).Distinct().ToList();
    locations.RemoveAll(l => !wanted.Contains(l.LocationId));
    foreach (var locationId in wanted.Where(id => locations.All(l => l.LocationId != id)))
    {
      locations.Add(new CompanyLocation(Id, locationId));
    }
  }

  public void ChangeSlug(string slug)
  {
    Slug = Normalize.Slug(slug);
  }

  public CompanyDomain AddDomain(string domain)
  {
    var value = Normalize.Domain(domain);
    var existing = domains.FirstOrDefault(d => d.Value == value);
    if (existing is not null)
    {
      return existing;
    }

    var added = new CompanyDomain(Id, value);
    domains.Add(added);
    return added;
  }

  public void RemoveDomain(string domain)
  {
    var value = Normalize.Domain(domain);
    domains.RemoveAll(d => d.Value == value);
  }

  public CompanyAlias AddAlias(string alias)
  {
    var value = Normalize.Alias(alias);
    var existing = aliases.FirstOrDefault(a => a.Value == value);
    if (existing is not null)
    {
      return existing;
    }

    var added = new CompanyAlias(Id, value);
    aliases.Add(added);
    return added;
  }

  public void RemoveAlias(string alias)
  {
    var value = Normalize.Alias(alias);
    aliases.RemoveAll(a => a.Value == value);
  }

  public Company(string slug, string name)
  {
    Id = Guid.CreateVersion7();
    Slug = Normalize.Slug(slug);
    Name = name;
  }
}
