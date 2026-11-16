using System.Reflection;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Catalogs;
using Karajob.Domain.Common;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Goals;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Karajob.Infrastructure.Persistence;

public class KarajobDbContext(DbContextOptions<KarajobDbContext> options, ICurrentUser currentUser) : DbContext(options), IKarajobDbContext
{
  public const string UserFilterName = "UserId";

  private static readonly MethodInfo ApplyUserFilterMethod = typeof(KarajobDbContext)
    .GetMethod(nameof(ApplyUserFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

  // Read per query, not in the constructor: migrations and the seed run in a scope with no profile selected.
  private Guid CurrentUserId => currentUser.UserId;

  public DbSet<User> Users => Set<User>();
  public DbSet<Company> Companies => Set<Company>();
  public DbSet<CompanyDomain> CompanyDomains => Set<CompanyDomain>();
  public DbSet<CompanyAlias> CompanyAliases => Set<CompanyAlias>();
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<UserCompanyNote> UserCompanyNotes => Set<UserCompanyNote>();
  public DbSet<Role> Roles => Set<Role>();
  public DbSet<Location> Locations => Set<Location>();
  public DbSet<Language> Languages => Set<Language>();
  public DbSet<Platform> Platforms => Set<Platform>();
  public DbSet<Hypothesis> Hypotheses => Set<Hypothesis>();
  public DbSet<HypothesisLocation> HypothesisLocations => Set<HypothesisLocation>();
  public DbSet<HypothesisPlatform> HypothesisPlatforms => Set<HypothesisPlatform>();
  public DbSet<CvDocument> CvDocuments => Set<CvDocument>();
  public DbSet<CvVersion> CvVersions => Set<CvVersion>();
  public DbSet<Vacancy> Vacancies => Set<Vacancy>();
  public DbSet<VacancyMatch> VacancyMatches => Set<VacancyMatch>();
  public DbSet<VacancyPlatform> VacancyPlatforms => Set<VacancyPlatform>();
  public DbSet<JobApplication> JobApplications => Set<JobApplication>();
  public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();
  public DbSet<SearchGoal> SearchGoals => Set<SearchGoal>();
  public DbSet<SearchGoalLocation> SearchGoalLocations => Set<SearchGoalLocation>();
  public DbSet<SearchGoalPlatform> SearchGoalPlatforms => Set<SearchGoalPlatform>();

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    // Without it SalaryRange is discovered as a keyless entity type before VacancyConfiguration maps it as complex.
    configurationBuilder.ComplexProperties<SalaryRange>();
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(KarajobDbContext).Assembly);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
    {
      // Ids come from Guid.CreateVersion7() in the domain. With store-generated keys EF treats new children
      // found through a navigation (e.g. a new ApplicationEvent) as existing rows and UPDATEs them.
      entityType.FindPrimaryKey()!.Properties.Single().ValueGenerated = ValueGenerated.Never;

      if (!typeof(IUserOwned).IsAssignableFrom(entityType.ClrType)) continue;

      modelBuilder.Entity(entityType.ClrType)
        .HasOne(typeof(User))
        .WithMany()
        .HasForeignKey(nameof(IUserOwned.UserId))
        .OnDelete(DeleteBehavior.Restrict);
      ApplyUserFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
    }
  }

  private void ApplyUserFilter<TEntity>(ModelBuilder modelBuilder)
    where TEntity : class, IUserOwned
  {
    modelBuilder.Entity<TEntity>().HasQueryFilter(UserFilterName, e => e.UserId == CurrentUserId);
  }
}
