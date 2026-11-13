using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Persistence;

public interface IKarajobDbContext : IAsyncDisposable
{
  DbSet<User> Users { get; }
  DbSet<Company> Companies { get; }
  DbSet<CompanyDomain> CompanyDomains { get; }
  DbSet<CompanyAlias> CompanyAliases { get; }
  DbSet<Project> Projects { get; }
  DbSet<UserCompanyNote> UserCompanyNotes { get; }
  DbSet<Hypothesis> Hypotheses { get; }
  DbSet<CvDocument> CvDocuments { get; }
  DbSet<CvVersion> CvVersions { get; }
  DbSet<Vacancy> Vacancies { get; }
  DbSet<VacancyMatch> VacancyMatches { get; }
  DbSet<JobApplication> JobApplications { get; }
  DbSet<ApplicationEvent> ApplicationEvents { get; }

  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
