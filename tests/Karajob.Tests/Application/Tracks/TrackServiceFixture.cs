using Karajob.Application.Tracks;
using Karajob.Domain.Tracks;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Tracks;

public abstract class TrackServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  internal FixedTimeProvider Clock { get; } = new(Now);
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected TrackService Service { get; private set; } = null!;

  protected static TrackInput Input(
    string name = "Unity Developer RU",
    Guid? roleId = null,
    IReadOnlyList<Guid>? locationIds = null,
    IReadOnlyList<string>? industries = null,
    WorkFormat workFormat = WorkFormat.Remote,
    Guid? languageId = null,
    string? positioning = null,
    Guid? primaryCvDocumentId = null,
    TrackStatus status = TrackStatus.Draft,
    IReadOnlyList<Guid>? platformIds = null,
    IReadOnlyList<Guid>? engineIds = null)
  {
    return new TrackInput(
      name,
      roleId ?? SeedData.UnityDeveloperRoleId,
      locationIds ?? [],
      platformIds ?? [],
      engineIds ?? [],
      industries ?? [],
      workFormat,
      languageId ?? SeedData.RussianLanguageId,
      positioning,
      primaryCvDocumentId,
      status);
  }

  protected async Task<Guid> CreateWithStatusAsync(string name, TrackStatus status)
  {
    var created = await Service.CreateAsync(Input(name: name));
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var stored = await context.Tracks.SingleAsync(h => h.Id == created.Id);
    stored.Status = status;
    await context.SaveChangesAsync();
    return created.Id;
  }

  protected TrackService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateTrackService(Database, userId, Clock);
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
