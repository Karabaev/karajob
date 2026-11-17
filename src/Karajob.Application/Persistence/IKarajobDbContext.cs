using Karajob.Domain.Applications;
using Karajob.Domain.Catalogs;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Goals;
using Karajob.Domain.Tracks;
using Karajob.Domain.MasterProfiles;
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
  DbSet<CompanyLocation> CompanyLocations { get; }
  DbSet<Project> Projects { get; }
  DbSet<UserCompanyNote> UserCompanyNotes { get; }
  DbSet<Role> Roles { get; }
  DbSet<Location> Locations { get; }
  DbSet<Language> Languages { get; }
  DbSet<Platform> Platforms { get; }
  DbSet<Currency> Currencies { get; }
  DbSet<Engine> Engines { get; }
  DbSet<Track> Tracks { get; }
  DbSet<TrackLocation> TrackLocations { get; }
  DbSet<TrackPlatform> TrackPlatforms { get; }
  DbSet<TrackEngine> TrackEngines { get; }
  DbSet<TrackCheckIn> TrackCheckIns { get; }
  DbSet<TrackDecision> TrackDecisions { get; }
  DbSet<CvDocument> CvDocuments { get; }
  DbSet<CvVersion> CvVersions { get; }
  DbSet<Tag> Tags { get; }
  DbSet<CvDocumentTag> CvDocumentTags { get; }
  DbSet<Vacancy> Vacancies { get; }
  DbSet<VacancyMatch> VacancyMatches { get; }
  DbSet<VacancyPlatform> VacancyPlatforms { get; }
  DbSet<VacancyEngine> VacancyEngines { get; }
  DbSet<JobApplication> JobApplications { get; }
  DbSet<ApplicationEvent> ApplicationEvents { get; }
  DbSet<SearchGoal> SearchGoals { get; }
  DbSet<SearchGoalLocation> SearchGoalLocations { get; }
  DbSet<SearchGoalPlatform> SearchGoalPlatforms { get; }
  DbSet<SearchGoalEngine> SearchGoalEngines { get; }
  DbSet<MasterProfile> MasterProfiles { get; }
  DbSet<MasterProfileVersion> MasterProfileVersions { get; }

  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
