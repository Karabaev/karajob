using Karajob.Domain.Common;

namespace Karajob.Domain.Companies;

public class CompanyAlias
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public string Value { get; private set; }

  internal CompanyAlias(Guid companyId, string value)
  {
    Id = Guid.CreateVersion7();
    CompanyId = Require.NotEmpty(companyId);
    Value = Require.NotBlank(value);
  }
}
