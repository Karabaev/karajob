using Karajob.Domain.Common;

namespace Karajob.Domain.Companies;

public class Project
{
  public Guid Id { get; private set; }
  public Guid CompanyId { get; private set; }
  public string Name { get; set; }
  public string? Genre { get; set; }
  public string? Engine { get; set; }
  public ProjectStatus Status { get; set; }
  public string? Url { get; set; }

  public Project(Guid companyId, string name, ProjectStatus status)
  {
    Id = Guid.CreateVersion7();
    CompanyId = Require.NotEmpty(companyId);
    Name = Require.NotBlank(name);
    Status = status;
  }
}
