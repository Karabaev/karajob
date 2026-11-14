using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Normalization;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Vacancies;

public partial class VacancyService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, TimeProvider timeProvider)
{
  private static readonly TimeSpan SimilarWindow = TimeSpan.FromDays(30);

  public async Task<IReadOnlyList<VacancyListItem>> ListAsync(VacancyStage stage, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancies = await db.Vacancies.AsNoTracking().Where(v => v.Stage == stage).ToListAsync(cancellationToken);
    var companyIds = vacancies.Where(v => v.CompanyId is not null).Select(v => v.CompanyId!.Value).Distinct().ToList();
    var roleIds = vacancies.Where(v => v.RoleId is not null).Select(v => v.RoleId!.Value).Distinct().ToList();
    var companyNames = await db.Companies.AsNoTracking()
      .Where(c => companyIds.Contains(c.Id))
      .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    var roleNames = await db.Roles.AsNoTracking()
      .Where(r => roleIds.Contains(r.Id))
      .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

    return vacancies
      .OrderByDescending(v => v.DiscoveredAt)
      .Select(v => new VacancyListItem(
        v.Id,
        v.Url,
        v.CompanyId is { } companyId ? companyNames[companyId] : null,
        v.RoleId is { } roleId ? roleNames[roleId] : null,
        v.Stage,
        v.DiscoveredAt,
        v.Notes))
      .ToList();
  }

  public async Task<int> CountAsync(VacancyStage stage, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await db.Vacancies.CountAsync(v => v.Stage == stage, cancellationToken);
  }

  public async Task<QuickAddResult> QuickAddAsync(QuickAddInput input, CancellationToken cancellationToken = default)
  {
    string url;
    try
    {
      url = Normalize.VacancyUrl(input.Url);
    }
    catch (ArgumentException)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [nameof(QuickAddInput.Url)] = "Некорректная ссылка на вакансию",
      });
    }

    DbUpdateException saveError;
    await using (var db = await dbContextFactory.CreateDbContextAsync(cancellationToken))
    {
      if (await FindByUrlAsync(db, url, cancellationToken) is { } existing) return existing;

      var vacancy = new Vacancy(currentUser.UserId, url, VacancySource.Manual, timeProvider.GetUtcNow().UtcDateTime)
      {
        Notes = BlankToNull(input.Note),
      };
      db.Vacancies.Add(vacancy);
      try
      {
        await db.SaveChangesAsync(cancellationToken);
        return new QuickAddResult(vacancy.Id, false, vacancy.Stage);
      }
      catch (DbUpdateException ex)
      {
        // Usually the same URL was added concurrently (another tab or device) and the unique index rejected
        // this insert; the provider-specific error code is not visible here, so look the URL up again.
        saveError = ex;
      }
    }

    await using var retryDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await FindByUrlAsync(retryDb, url, cancellationToken)
      ?? throw new InvalidOperationException($"Saving vacancy {url} failed, and no existing vacancy has this URL.", saveError);
  }

  public async Task MoveToBoardAsync(Guid vacancyId, IReadOnlyList<Guid> hypothesisIds, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    EnsureStage(vacancy, VacancyStage.Inbox);

    var ids = hypothesisIds.Distinct().ToList();
    var error = ids.Count == 0
      ? "Выберите хотя бы одну гипотезу"
      : await FindHypothesesErrorAsync(db, ids, vacancy, cancellationToken);
    if (error is not null)
    {
      throw new ValidationFailedException(new Dictionary<string, string> { [nameof(VacancyInput.HypothesisIds)] = error });
    }

    ReplaceMatches(vacancy, ids);
    vacancy.MoveTo(VacancyStage.Targeted, timeProvider.GetUtcNow().UtcDateTime);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task DismissAsync(
    Guid vacancyId,
    DismissInput input,
    VacancyStage? expectedStage = null,
    CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    if (expectedStage is { } stage)
    {
      EnsureStage(vacancy, stage);
    }

    try
    {
      vacancy.Dismiss(input.Reason, BlankToNull(input.Comment), timeProvider.GetUtcNow().UtcDateTime);
    }
    catch (StageTransitionException)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = "Вакансию на этой стадии нельзя отклонить",
      });
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<VacancyDetails?> FindAsync(Guid vacancyId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await db.Vacancies.AsNoTracking()
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
    return vacancy is null ? null : await BuildDetailsAsync(db, vacancy, cancellationToken);
  }

  public async Task<VacancyDetails> CreateAsync(VacancyInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalized = await ValidateAsync(db, input, Guid.Empty, null, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    var vacancy = new Vacancy(currentUser.UserId, normalized.Url, VacancySource.Manual, now);
    Apply(vacancy, normalized);

    db.Vacancies.Add(vacancy);
    await db.SaveChangesAsync(cancellationToken);
    return await BuildDetailsAsync(db, vacancy, cancellationToken);
  }

  public async Task<VacancyDetails> UpdateAsync(Guid vacancyId, VacancyInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    var normalized = await ValidateAsync(db, input, vacancyId, vacancy, cancellationToken);

    vacancy.ChangeUrl(normalized.Url);
    Apply(vacancy, normalized);

    await db.SaveChangesAsync(cancellationToken);
    return await BuildDetailsAsync(db, vacancy, cancellationToken);
  }

  public async Task<IReadOnlyList<SimilarVacancy>> FindSimilarAsync(
    Guid companyId,
    Guid roleId,
    Guid? excludeVacancyId,
    CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var reference = timeProvider.GetUtcNow().UtcDateTime;
    if (excludeVacancyId is { } excludedId)
    {
      reference = await db.Vacancies.AsNoTracking()
        .Where(v => v.Id == excludedId)
        .Select(v => v.DiscoveredAt)
        .FirstOrDefaultAsync(cancellationToken);
      if (reference == default)
      {
        throw new KeyNotFoundException($"Vacancy {excludedId} was not found.");
      }
    }

    return await FindSimilarAsync(db, companyId, roleId, excludeVacancyId ?? Guid.Empty, reference, cancellationToken);
  }

  public async Task<TimelineItem> AddEventAsync(Guid vacancyId, VacancyEventInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);

    UtcGuard.Ensure(input.OccurredAt);

    ApplicationEvent added;
    try
    {
      added = vacancy.AddEvent(input.Type, input.OccurredAt, BlankToNull(input.Note));
    }
    catch (ArgumentException ex) when (ex.ParamName == "at")
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        ["OccurredAt"] = "Дата события раньше даты, когда вакансия найдена",
      });
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
      throw new ValidationFailedException(new Dictionary<string, string> { [nameof(VacancyEventInput.Type)] = ex.Message });
    }

    await db.SaveChangesAsync(cancellationToken);
    return ToItem(added);
  }

  public async Task DeleteEventAsync(Guid vacancyId, Guid eventId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);

    try
    {
      vacancy.RemoveEvent(eventId);
    }
    catch (InvalidOperationException)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = "Переходы между стадиями удалять нельзя",
      });
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  private static async Task<Vacancy> FindVacancyAsync(IKarajobDbContext db, Guid vacancyId, CancellationToken cancellationToken)
  {
    return await db.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken)
      ?? throw new KeyNotFoundException($"Vacancy {vacancyId} was not found.");
  }

  private static void Apply(Vacancy vacancy, NormalizedVacancy normalized)
  {
    vacancy.CompanyId = normalized.CompanyId;
    vacancy.RoleId = normalized.RoleId;
    vacancy.LocationId = normalized.LocationId;
    vacancy.LanguageId = normalized.LanguageId;
    vacancy.WorkFormat = normalized.WorkFormat;
    vacancy.Salary = normalized.Salary;
    vacancy.Description = normalized.Description;
    vacancy.Notes = normalized.Notes;
    ReplaceMatches(vacancy, normalized.HypothesisIds);
  }

  // The UI acts on a list that may be stale (another tab or device moved the vacancy meanwhile).
  private static void EnsureStage(Vacancy vacancy, VacancyStage expected)
  {
    if (vacancy.Stage == expected) return;

    throw new ValidationFailedException(new Dictionary<string, string>
    {
      [string.Empty] = expected == VacancyStage.Inbox ? "Вакансия уже не в Inbox" : "Стадия вакансии уже изменилась",
    });
  }

  private static void ReplaceMatches(Vacancy vacancy, List<Guid> hypothesisIds)
  {
    foreach (var hypothesisId in hypothesisIds.Where(id => vacancy.Matches.All(m => m.HypothesisId != id)))
    {
      vacancy.SetMatch(hypothesisId, null, null, MatchAuthor.Human);
    }

    foreach (var hypothesisId in vacancy.Matches.Select(m => m.HypothesisId).Where(id => !hypothesisIds.Contains(id)).ToList())
    {
      vacancy.RemoveMatch(hypothesisId);
    }
  }

  private static async Task<QuickAddResult?> FindByUrlAsync(IKarajobDbContext db, string url, CancellationToken cancellationToken)
  {
    return await db.Vacancies.AsNoTracking()
      .Where(v => v.Url == url)
      .Select(v => new QuickAddResult(v.Id, true, v.Stage))
      .FirstOrDefaultAsync(cancellationToken);
  }

  private static async Task<string?> FindHypothesesErrorAsync(
    IKarajobDbContext db,
    List<Guid> hypothesisIds,
    Vacancy? existing,
    CancellationToken cancellationToken)
  {
    var knownHypotheses = await db.Hypotheses
      .Where(h => hypothesisIds.Contains(h.Id))
      .Select(h => new { h.Id, h.Status })
      .ToListAsync(cancellationToken);
    if (knownHypotheses.Count != hypothesisIds.Count)
    {
      return "Гипотеза не найдена";
    }

    var addsDisabled = knownHypotheses.Any(h => h.Status == HypothesisStatus.Disabled
      && (existing is null || existing.Matches.All(m => m.HypothesisId != h.Id)));
    return addsDisabled ? "Гипотеза отключена" : null;
  }

  private async Task<NormalizedVacancy> ValidateAsync(
    IKarajobDbContext db,
    VacancyInput input,
    Guid excludedVacancyId,
    Vacancy? existing,
    CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    string? url = null;
    try
    {
      url = Normalize.VacancyUrl(input.Url);
    }
    catch (ArgumentException)
    {
      errors[nameof(VacancyInput.Url)] = "Некорректная ссылка на вакансию";
    }

    if (url is not null && await db.Vacancies.AnyAsync(v => v.Id != excludedVacancyId && v.Url == url, cancellationToken))
    {
      errors[nameof(VacancyInput.Url)] = "Вакансия с этой ссылкой уже есть";
    }

    if (input.CompanyId is { } companyId && !await db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
    {
      errors[nameof(VacancyInput.CompanyId)] = "Компания не найдена";
    }

    if (input.RoleId is { } roleId && !await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(VacancyInput.RoleId)] = "Роль не найдена";
    }

    if (input.LocationId is { } locationId && !await db.Locations.AnyAsync(l => l.Id == locationId, cancellationToken))
    {
      errors[nameof(VacancyInput.LocationId)] = "Локация не найдена";
    }

    if (input.LanguageId is { } languageId && !await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(VacancyInput.LanguageId)] = "Язык не найден";
    }

    SalaryRange? salary = null;
    if (input.SalaryMin is not null || input.SalaryMax is not null)
    {
      try
      {
        salary = new SalaryRange(input.SalaryMin, input.SalaryMax, input.SalaryCurrency?.Trim() ?? string.Empty);
      }
      catch (ArgumentException)
      {
        errors["Salary"] = "Некорректная вилка: значения неотрицательные, минимум не больше максимума, валюта из 3 букв";
      }
    }

    var hypothesisIds = input.HypothesisIds.Distinct().ToList();
    var hypothesesError = await FindHypothesesErrorAsync(db, hypothesisIds, existing, cancellationToken);
    if (hypothesesError is null && existing is not null)
    {
      hypothesesError = FindMatchRemovalError(existing, hypothesisIds);
    }

    if (hypothesesError is not null)
    {
      errors[nameof(VacancyInput.HypothesisIds)] = hypothesesError;
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedVacancy(
      url!,
      input.CompanyId,
      input.RoleId,
      input.LocationId,
      input.LanguageId,
      input.WorkFormat is null or WorkFormat.None ? null : input.WorkFormat,
      salary,
      hypothesisIds,
      BlankToNull(input.Description),
      BlankToNull(input.Notes));
  }

  private static string? FindMatchRemovalError(Vacancy vacancy, List<Guid> hypothesisIds)
  {
    var removed = vacancy.Matches.Select(m => m.HypothesisId).Where(id => !hypothesisIds.Contains(id)).ToList();
    if (vacancy.Application is { } application && removed.Contains(application.HypothesisId))
    {
      return "Нельзя снять гипотезу, по которой сделан отклик";
    }

    var onBoard = vacancy.Stage is not (VacancyStage.Inbox or VacancyStage.Dismissed);
    if (onBoard && removed.Count > 0 && hypothesisIds.Count == 0)
    {
      return "Вакансии на доске нужна хотя бы одна гипотеза";
    }

    return null;
  }

  private async Task<VacancyDetails> BuildDetailsAsync(IKarajobDbContext db, Vacancy vacancy, CancellationToken cancellationToken)
  {
    string? companyName = null;
    if (vacancy.CompanyId is { } companyId)
    {
      companyName = await db.Companies.AsNoTracking()
        .Where(c => c.Id == companyId)
        .Select(c => c.Name)
        .FirstAsync(cancellationToken);
    }

    IReadOnlyList<SimilarVacancy> similar = [];
    if (vacancy.CompanyId is { } similarCompanyId && vacancy.RoleId is { } similarRoleId)
    {
      similar = await FindSimilarAsync(db, similarCompanyId, similarRoleId, vacancy.Id, vacancy.DiscoveredAt, cancellationToken);
    }

    return new VacancyDetails(
      vacancy.Id,
      vacancy.Url,
      vacancy.CompanyId,
      companyName,
      vacancy.RoleId,
      vacancy.LocationId,
      vacancy.LanguageId,
      vacancy.WorkFormat,
      vacancy.Salary?.Min,
      vacancy.Salary?.Max,
      vacancy.Salary?.Currency,
      vacancy.Matches.Select(m => m.HypothesisId).ToList(),
      vacancy.Description,
      vacancy.Notes,
      vacancy.Source,
      vacancy.Stage,
      vacancy.DismissReason,
      vacancy.DiscoveredAt,
      vacancy.Application?.HypothesisId,
      vacancy.Events.OrderByDescending(e => e.OccurredAt).Select(ToItem).ToList(),
      similar);
  }

  private static async Task<IReadOnlyList<SimilarVacancy>> FindSimilarAsync(
    IKarajobDbContext db,
    Guid companyId,
    Guid roleId,
    Guid excludedVacancyId,
    DateTime reference,
    CancellationToken cancellationToken)
  {
    var from = reference - SimilarWindow;
    var to = reference + SimilarWindow;
    var candidates = await db.Vacancies.AsNoTracking()
      .Where(v => v.CompanyId == companyId && v.RoleId == roleId && v.Id != excludedVacancyId
        && v.DiscoveredAt >= from && v.DiscoveredAt <= to)
      .ToListAsync(cancellationToken);

    return candidates
      .OrderByDescending(v => v.DiscoveredAt)
      .Select(v => new SimilarVacancy(v.Id, v.Url, v.Stage, v.DiscoveredAt))
      .ToList();
  }

  private static TimelineItem ToItem(ApplicationEvent e)
  {
    return new TimelineItem(e.Id, e.Type, e.OccurredAt, e.FromStage, e.ToStage, e.Note);
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record NormalizedVacancy(
    string Url,
    Guid? CompanyId,
    Guid? RoleId,
    Guid? LocationId,
    Guid? LanguageId,
    WorkFormat? WorkFormat,
    SalaryRange? Salary,
    List<Guid> HypothesisIds,
    string? Description,
    string? Notes);
}
