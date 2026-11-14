using Karajob.Application.Cvs;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Cvs;

public abstract class CvServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
  protected static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 1, 2, 3];

  internal FixedTimeProvider Clock { get; } = new(Now);
  internal InMemoryFileStorage Storage { get; } = new();
  protected CvOptions Options { get; } = new() { MaxFileSizeMb = 5 };
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected CvService Service { get; private set; } = null!;

  protected static CvInput Input(
    string name = "Main CV",
    Guid? languageId = null,
    IReadOnlyList<string>? tags = null)
  {
    return new CvInput(name, languageId ?? SeedData.RussianLanguageId, tags ?? []);
  }

  protected static CvInput NoLanguageInput(string name = "Main CV")
  {
    return new CvInput(name, null, []);
  }

  protected static CvUpload Upload(
    string fileName = "cv.pdf",
    byte[]? bytes = null,
    long? sizeBytes = null,
    string? comment = null,
    Action? onOpen = null)
  {
    var content = bytes ?? PdfBytes;
    return new CvUpload(
      fileName,
      sizeBytes ?? content.Length,
      () =>
      {
        onOpen?.Invoke();
        return new MemoryStream(content);
      },
      comment);
  }

  protected CvService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateCvService(Database, userId, Storage, Clock, Options);
  }

  protected async Task<Guid> AddApplicationAsync(Guid userId, Guid cvVersionId)
  {
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(Database, userId);
    var vacancy = new Vacancy(userId, "https://example.com/jobs/cv-ref", VacancySource.Manual, Now);
    vacancy.SetMatch(hypothesis.Id, 80, "fits", MatchAuthor.Human);
    vacancy.MoveTo(VacancyStage.Targeted, Now);
    vacancy.MoveTo(VacancyStage.Preparing, Now);
    vacancy.Apply(hypothesis.Id, cvVersionId, ApplicationChannel.Website, Now, "letter");
    await using var context = Database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
    return vacancy.Id;
  }

  protected async Task AddHypothesisWithPrimaryCvAsync(Guid userId, Guid cvDocumentId)
  {
    var hypothesis = PersistenceFixtures.NewHypothesis(userId);
    hypothesis.PrimaryCvDocumentId = cvDocumentId;
    await using var context = Database.CreateContext(userId);
    context.Hypotheses.Add(hypothesis);
    await context.SaveChangesAsync();
  }

  protected async Task<int> CountDocumentsAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.CvDocuments.CountAsync();
  }

  protected async Task<int> CountVersionsAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.CvVersions.CountAsync();
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
