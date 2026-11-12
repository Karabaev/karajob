using Karajob.Domain.Common;

namespace Karajob.Domain.Companies;

public class UserCompanyNote : IUserOwned
{
  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public Guid CompanyId { get; private set; }
  public string? Note { get; set; }
  public bool IsBlacklisted { get; set; }

  public UserCompanyNote(Guid userId, Guid companyId)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    CompanyId = Require.NotEmpty(companyId);
  }
}
