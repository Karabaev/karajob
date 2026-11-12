namespace Karajob.Domain.Companies;

public class CompanyDomain
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public string Value { get; private set; }

  internal CompanyDomain(Guid companyId, string value) => throw new NotImplementedException();
}
