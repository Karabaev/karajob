using Karajob.Application.Persistence;
using Karajob.Application.Users;

namespace Karajob.Application.Companies;

public class CompanyService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser)
{
  public Task<IReadOnlyList<CompanyListItem>> SearchAsync(string? query, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CompanyDetails?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CompanyDetails> CreateAsync(CompanyInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CompanyDetails> UpdateAsync(Guid companyId, CompanyInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<ProjectItem> AddProjectAsync(Guid companyId, ProjectInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<ProjectItem> UpdateProjectAsync(Guid projectId, ProjectInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task SaveNoteAsync(Guid companyId, string? note, bool isBlacklisted, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
