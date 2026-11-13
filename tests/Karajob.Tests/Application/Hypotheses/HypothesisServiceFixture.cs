using Karajob.Application.Hypotheses;
using Karajob.Domain.Hypotheses;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public abstract class HypothesisServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  internal FixedTimeProvider Clock { get; } = new(Now);
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected HypothesisService Service { get; private set; } = null!;

  protected static HypothesisInput Input(
    string name = "Unity Developer RU",
    string role = "Unity Developer",
    IReadOnlyList<string>? locations = null,
    IReadOnlyList<string>? industries = null,
    WorkFormat workFormat = WorkFormat.Remote,
    string language = "ru",
    string? positioning = null,
    Guid? primaryCvDocumentId = null,
    HypothesisStatus status = HypothesisStatus.Draft)
  {
    return new HypothesisInput(name, role, locations ?? [], industries ?? [], workFormat, language, positioning, primaryCvDocumentId, status);
  }

  protected HypothesisService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateHypothesisService(Database, userId, Clock);
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
