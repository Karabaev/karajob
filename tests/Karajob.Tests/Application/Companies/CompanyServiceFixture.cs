using Karajob.Application.Companies;
using Karajob.Domain.Companies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Companies;

public abstract class CompanyServiceFixture : IAsyncLifetime
{
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected CompanyService Service { get; private set; } = null!;

  protected static CompanyInput Input(
    string slug = "pixel-forge",
    string name = "Pixel Forge",
    string? website = null,
    string? description = null,
    IReadOnlyList<string>? domains = null,
    IReadOnlyList<string>? aliases = null)
  {
    return new CompanyInput(slug, name, website, description, domains ?? [], aliases ?? []);
  }

  protected static ProjectInput NewProject(
    string name = "Dragon Quest",
    string? genre = null,
    string? engine = null,
    ProjectStatus status = ProjectStatus.InDevelopment,
    string? url = null)
  {
    return new ProjectInput(name, genre, engine, status, url);
  }

  protected CompanyService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateCompanyService(Database, userId);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = ServiceFor(SeedData.OwnerId);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
