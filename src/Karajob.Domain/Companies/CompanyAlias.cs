namespace Karajob.Domain.Companies;

public class CompanyAlias
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public string Value { get; private set; }

  internal CompanyAlias(Guid companyId, string value) => throw new NotImplementedException();
}
