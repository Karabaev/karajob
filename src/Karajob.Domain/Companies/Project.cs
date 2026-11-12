using Karajob.Domain.Common;
using Karajob.Domain.Normalization;

namespace Karajob.Domain.Companies;

public class Project
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public string Name { get; set => field = Require.NotBlank(value); }
  public string? Genre { get; set; }
  public string? Engine { get; set; }
  public ProjectStatus Status { get; set; }
  public string? Url { get; set => field = value is null ? null : Normalize.WebUrl(value); }

  public Project(Guid companyId, string name, ProjectStatus status)
  {
    Id = Guid.CreateVersion7();
    CompanyId = Require.NotEmpty(companyId);
    Name = name;
    Status = status;
  }
}
