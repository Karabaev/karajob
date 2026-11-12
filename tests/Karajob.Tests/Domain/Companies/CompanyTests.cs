using Karajob.Domain.Companies;

namespace Karajob.Tests.Domain.Companies;

public class CompanyTests
{
  [Fact]
  public void Ctor_Valid_SetsNormalizedSlugNameAndId()
  {
    var company = new Company(" Ubisoft ", "Ubisoft");

    Assert.Equal("ubisoft", company.Slug);
    Assert.Equal("Ubisoft", company.Name);
    Assert.NotEqual(Guid.Empty, company.Id);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => new Company("ubisoft", name));
  }

  [Fact]
  public void Ctor_InvalidSlug_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Company("ubi soft", "Ubisoft"));
  }

  [Fact]
  public void ChangeSlug_Valid_NormalizesSlug()
  {
    var company = new Company("ubisoft", "Ubisoft");

    company.ChangeSlug("Ubisoft-Montreal");

    Assert.Equal("ubisoft-montreal", company.Slug);
  }

  [Fact]
  public void ChangeSlug_Invalid_ThrowsAndKeepsOldSlug()
  {
    var company = new Company("ubisoft", "Ubisoft");

    Assert.ThrowsAny<ArgumentException>(() => company.ChangeSlug("ubi soft"));
    Assert.Equal("ubisoft", company.Slug);
  }

  [Fact]
  public void AddDomain_Url_AddsNormalizedDomainLinkedToCompany()
  {
    var company = new Company("ubisoft", "Ubisoft");

    var domain = company.AddDomain("https://www.Ubisoft.com/");

    var single = Assert.Single(company.Domains);
    Assert.Same(domain, single);
    Assert.Equal("ubisoft.com", domain.Value);
    Assert.Equal(company.Id, domain.CompanyId);
    Assert.NotEqual(Guid.Empty, domain.Id);
  }

  [Fact]
  public void AddDomain_SameDomainDifferentCasing_ReturnsExistingInstance()
  {
    var company = new Company("ubisoft", "Ubisoft");

    var first = company.AddDomain("ubisoft.com");
    var second = company.AddDomain("UBISOFT.COM");

    Assert.Same(first, second);
    Assert.Single(company.Domains);
  }

  [Fact]
  public void RemoveDomain_CaseInsensitive_RemovesDomain()
  {
    var company = new Company("ubisoft", "Ubisoft");
    company.AddDomain("ubisoft.com");

    company.RemoveDomain("UBISOFT.COM");

    Assert.Empty(company.Domains);
  }

  [Fact]
  public void AddDomain_Invalid_Throws()
  {
    var company = new Company("ubisoft", "Ubisoft");

    Assert.ThrowsAny<ArgumentException>(() => company.AddDomain("not a domain"));
  }

  [Fact]
  public void AddAlias_Valid_AddsNormalizedAlias()
  {
    var company = new Company("ubisoft", "Ubisoft");

    var alias = company.AddAlias("  Ubisoft   Entertainment ");

    Assert.Equal("ubisoft entertainment", alias.Value);
    Assert.Equal(company.Id, alias.CompanyId);
    Assert.Single(company.Aliases);
  }

  [Fact]
  public void AddAlias_Duplicate_ReturnsExistingInstance()
  {
    var company = new Company("ubisoft", "Ubisoft");

    var first = company.AddAlias("Ubisoft Entertainment");
    var second = company.AddAlias("UBISOFT ENTERTAINMENT");

    Assert.Same(first, second);
    Assert.Single(company.Aliases);
  }

  [Fact]
  public void RemoveAlias_CaseInsensitive_RemovesAlias()
  {
    var company = new Company("ubisoft", "Ubisoft");
    company.AddAlias("ubisoft entertainment");

    company.RemoveAlias("UBISOFT Entertainment");

    Assert.Empty(company.Aliases);
  }
}
