using Karajob.Domain.Common;

namespace Karajob.Domain.Companies;

public class CompanyLocation
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public Guid LocationId { get; private set; }

  internal CompanyLocation(Guid companyId, Guid locationId)
  {
    Id = Guid.CreateVersion7();
    CompanyId = Require.NotEmpty(companyId);
    LocationId = Require.NotEmpty(locationId);
  }
}
