using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Infrastructure.Persistence;

public class KarajobDbContext(DbContextOptions<KarajobDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
  private readonly Guid currentUserId = currentUser.UserId;

  public DbSet<User> Users => Set<User>();
  public DbSet<Company> Companies => Set<Company>();
  public DbSet<CompanyDomain> CompanyDomains => Set<CompanyDomain>();
  public DbSet<CompanyAlias> CompanyAliases => Set<CompanyAlias>();
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<UserCompanyNote> UserCompanyNotes => Set<UserCompanyNote>();
  public DbSet<Hypothesis> Hypotheses => Set<Hypothesis>();
  public DbSet<CvDocument> CvDocuments => Set<CvDocument>();
  public DbSet<CvVersion> CvVersions => Set<CvVersion>();
  public DbSet<Vacancy> Vacancies => Set<Vacancy>();
  public DbSet<VacancyMatch> VacancyMatches => Set<VacancyMatch>();
  public DbSet<JobApplication> JobApplications => Set<JobApplication>();
  public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();
}
