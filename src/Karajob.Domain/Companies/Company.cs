namespace Karajob.Domain.Companies;

public class Company
{
  private readonly List<CompanyDomain> domains = [];
  private readonly List<CompanyAlias> aliases = [];

  public Guid Id { get; private set; }
  public string Slug { get; private set; }
  public string Name { get; set; }
  public string? Website { get; set; }
  public string? Description { get; set; }
  public string? IconPath { get; set; }
  public IReadOnlyCollection<CompanyDomain> Domains => domains;
  public IReadOnlyCollection<CompanyAlias> Aliases => aliases;

  public void ChangeSlug(string slug) => throw new NotImplementedException();

  public CompanyDomain AddDomain(string domain) => throw new NotImplementedException();

  public void RemoveDomain(string domain) => throw new NotImplementedException();

  public CompanyAlias AddAlias(string alias) => throw new NotImplementedException();

  public void RemoveAlias(string alias) => throw new NotImplementedException();

  public Company(string slug, string name) => throw new NotImplementedException();
}
