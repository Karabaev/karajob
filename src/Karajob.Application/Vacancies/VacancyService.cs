using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Tracks;
using Karajob.Domain.Normalization;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Vacancies;

public partial class VacancyService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  IPageTitleReader titleReader,
  IStringLocalizer<VacancyMessages> localizer)
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
        v.Title,
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
    var url = NormalizeUrlOrFail(input.Url);
    await using (var lookupDb = await dbContextFactory.CreateDbContextAsync(cancellationToken))
    {
      if (await FindByUrlAsync(lookupDb, url, cancellationToken) is { } existing) return existing;
    }

    var title = CleanTitle(input.Title) ?? await ReadTitleAsync(url, cancellationToken);
    DbUpdateException saveError;
    await using (var db = await dbContextFactory.CreateDbContextAsync(cancellationToken))
    {
      var vacancy = new Vacancy(currentUser.UserId, url, VacancySource.Manual, timeProvider.GetUtcNow().UtcDateTime)
      {
        Title = title,
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

  public async Task<string?> FetchTitleAsync(string url, CancellationToken cancellationToken = default)
  {
    return await ReadTitleAsync(NormalizeUrlOrFail(url), cancellationToken);
  }

  public async Task MoveToBoardAsync(Guid vacancyId, IReadOnlyList<Guid> trackIds, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    EnsureStage(vacancy, VacancyStage.Inbox);

    var ids = trackIds.Distinct().ToList();
    var error = ids.Count == 0
      ? localizer["Error.TrackRequired"]
      : await FindTracksErrorAsync(db, ids, vacancy, vacancy.RoleId, cancellationToken);
    if (error is not null)
    {
      throw new ValidationFailedException(new Dictionary<string, string> { [nameof(VacancyInput.TrackIds)] = error });
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
        [string.Empty] = localizer["Error.CannotDismissAtStage"],
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
    NormalizedVacancy normalized;
    await using (var validationDb = await dbContextFactory.CreateDbContextAsync(cancellationToken))
    {
      normalized = await ValidateAsync(validationDb, input, Guid.Empty, null, cancellationToken);
    }

    // Read the page outside any DbContext: the request may take seconds and contexts are short-lived.
    if (normalized.Title is null)
    {
      normalized = normalized with { Title = await ReadTitleAsync(normalized.Url, cancellationToken) };
    }

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var vacancy = new Vacancy(currentUser.UserId, normalized.Url, VacancySource.Manual, now);
    Apply(vacancy, normalized);

    db.Vacancies.Add(vacancy);
    try
    {
      await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      if (!await UrlTakenAsync(normalized.Url, cancellationToken)) throw;

      // The same URL was added concurrently while the page title was being read.
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [nameof(VacancyInput.Url)] = localizer["Error.UrlExists"],
      });
    }

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
      added = vacancy.AddEvent(input.Type, input.OccurredAt, BlankToNull(input.Note), currentUser.TimeZone);
    }
    catch (ArgumentException ex) when (ex.ParamName == "at")
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        ["OccurredAt"] = localizer["Error.EventBeforeDiscovered"],
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
        [string.Empty] = localizer["Error.TransitionDeleteForbidden"],
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
    vacancy.Title = normalized.Title;
    vacancy.CompanyId = normalized.CompanyId;
    vacancy.RoleId = normalized.RoleId;
    vacancy.LocationId = normalized.LocationId;
    vacancy.SetPlatforms(normalized.PlatformIds);
    vacancy.SetEngines(normalized.EngineIds);
    vacancy.LanguageId = normalized.LanguageId;
    vacancy.WorkFormat = normalized.WorkFormat;
    vacancy.Salary = normalized.Salary;
    vacancy.Description = normalized.Description;
    vacancy.Notes = normalized.Notes;
    ReplaceMatches(vacancy, normalized.TrackIds);
  }

  // The UI acts on a list that may be stale (another tab or device moved the vacancy meanwhile).
  private void EnsureStage(Vacancy vacancy, VacancyStage expected)
  {
    if (vacancy.Stage == expected) return;

    throw new ValidationFailedException(new Dictionary<string, string>
    {
      [string.Empty] = expected == VacancyStage.Inbox ? localizer["Error.NotInInbox"] : localizer["Error.StageChanged"],
    });
  }

  private static void ReplaceMatches(Vacancy vacancy, List<Guid> trackIds)
  {
    foreach (var trackId in trackIds.Where(id => vacancy.Matches.All(m => m.TrackId != id)))
    {
      vacancy.SetMatch(trackId, null, null, MatchAuthor.Human);
    }

    foreach (var trackId in vacancy.Matches.Select(m => m.TrackId).Where(id => !trackIds.Contains(id)).ToList())
    {
      vacancy.RemoveMatch(trackId);
    }
  }

  private static async Task<QuickAddResult?> FindByUrlAsync(IKarajobDbContext db, string url, CancellationToken cancellationToken)
  {
    return await db.Vacancies.AsNoTracking()
      .Where(v => v.Url == url)
      .Select(v => new QuickAddResult(v.Id, true, v.Stage))
      .FirstOrDefaultAsync(cancellationToken);
  }

  private async Task<string?> FindTracksErrorAsync(
    IKarajobDbContext db,
    List<Guid> trackIds,
    Vacancy? existing,
    Guid? roleId,
    CancellationToken cancellationToken)
  {
    var knownTracks = await db.Tracks
      .Where(h => trackIds.Contains(h.Id))
      .Select(h => new { h.Id, h.Status, h.RoleId })
      .ToListAsync(cancellationToken);
    if (knownTracks.Count != trackIds.Count)
    {
      return localizer["Error.TrackNotFound"];
    }

    // The applied track can't be unselected, so exempting it keeps a vacancy with an application editable.
    var appliedId = existing?.Application?.TrackId;
    if (roleId is not null && knownTracks.Any(h => h.RoleId != roleId && h.Id != appliedId))
    {
      return localizer["Error.TrackRoleMismatch"];
    }

    var addsDisabled = knownTracks.Any(h => h.Status == TrackStatus.Disabled
      && (existing is null || existing.Matches.All(m => m.TrackId != h.Id)));
    return addsDisabled ? localizer["Error.TrackDisabled"].Value : null;
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
      errors[nameof(VacancyInput.Url)] = localizer["Error.UrlInvalid"];
    }

    if (url is not null && await db.Vacancies.AnyAsync(v => v.Id != excludedVacancyId && v.Url == url, cancellationToken))
    {
      errors[nameof(VacancyInput.Url)] = localizer["Error.UrlExists"];
    }

    if (input.CompanyId is { } companyId && !await db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
    {
      errors[nameof(VacancyInput.CompanyId)] = localizer["Error.CompanyNotFound"];
    }

    if (input.RoleId is { } roleId && !await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(VacancyInput.RoleId)] = localizer["Error.RoleNotFound"];
    }

    if (input.LocationId is { } locationId && !await db.Locations.AnyAsync(l => l.Id == locationId, cancellationToken))
    {
      errors[nameof(VacancyInput.LocationId)] = localizer["Error.LocationNotFound"];
    }

    if (input.LanguageId is { } languageId && !await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(VacancyInput.LanguageId)] = localizer["Error.LanguageNotFound"];
    }

    var platformIds = input.PlatformIds.Distinct().ToList();
    if (platformIds.Count > 0 && await db.Platforms.CountAsync(p => platformIds.Contains(p.Id), cancellationToken) != platformIds.Count)
    {
      errors[nameof(VacancyInput.PlatformIds)] = localizer["Error.PlatformNotFound"];
    }

    var engineIds = input.EngineIds.Distinct().ToList();
    if (engineIds.Count > 0 && await db.Engines.CountAsync(e => engineIds.Contains(e.Id), cancellationToken) != engineIds.Count)
    {
      errors[nameof(VacancyInput.EngineIds)] = localizer["Error.EngineNotFound"];
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
        errors["Salary"] = localizer["Error.SalaryInvalid"];
      }

      if (salary is { } range && !await db.Currencies.AnyAsync(c => c.Code == range.Currency, cancellationToken))
      {
        errors["Salary"] = localizer["Error.CurrencyNotFound"];
      }
    }

    var trackIds = input.TrackIds.Distinct().ToList();
    var tracksError = await FindTracksErrorAsync(db, trackIds, existing, input.RoleId, cancellationToken);
    if (tracksError is null && existing is not null)
    {
      tracksError = FindMatchRemovalError(existing, trackIds);
    }

    if (tracksError is not null)
    {
      errors[nameof(VacancyInput.TrackIds)] = tracksError;
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedVacancy(
      url!,
      CleanTitle(input.Title),
      input.CompanyId,
      input.RoleId,
      input.LocationId,
      platformIds,
      engineIds,
      input.LanguageId,
      input.WorkFormat is null or WorkFormat.None ? null : input.WorkFormat,
      salary,
      trackIds,
      BlankToNull(input.Description),
      BlankToNull(input.Notes));
  }

  private string? FindMatchRemovalError(Vacancy vacancy, List<Guid> trackIds)
  {
    var removed = vacancy.Matches.Select(m => m.TrackId).Where(id => !trackIds.Contains(id)).ToList();
    if (vacancy.Application is { } application && removed.Contains(application.TrackId))
    {
      return localizer["Error.CannotUnmatchApplied"];
    }

    var onBoard = vacancy.Stage is not (VacancyStage.Inbox or VacancyStage.Dismissed);
    if (onBoard && removed.Count > 0 && trackIds.Count == 0)
    {
      return localizer["Error.BoardNeedsTrack"];
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
      vacancy.Title,
      vacancy.CompanyId,
      companyName,
      vacancy.RoleId,
      vacancy.LocationId,
      vacancy.Platforms.Select(p => p.PlatformId).ToList(),
      vacancy.Engines.Select(e => e.EngineId).ToList(),
      vacancy.LanguageId,
      vacancy.WorkFormat,
      vacancy.Salary?.Min,
      vacancy.Salary?.Max,
      vacancy.Salary?.Currency,
      vacancy.Matches.Select(m => m.TrackId).ToList(),
      vacancy.Description,
      vacancy.Notes,
      vacancy.Source,
      vacancy.Stage,
      vacancy.DismissReason,
      vacancy.DiscoveredAt,
      vacancy.Application?.TrackId,
      vacancy.Application?.CoverLetter,
      vacancy.Events.OrderByDescending(e => e.OccurredAt).Select(ToItem).ToList(),
      similar,
      vacancy.ClosedFromStage,
      vacancy.ReopenTarget);
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

  private string NormalizeUrlOrFail(string url)
  {
    try
    {
      return Normalize.VacancyUrl(url);
    }
    catch (ArgumentException)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [nameof(QuickAddInput.Url)] = localizer["Error.UrlInvalid"],
      });
    }
  }

  private async Task<bool> UrlTakenAsync(string url, CancellationToken cancellationToken)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await db.Vacancies.AnyAsync(v => v.Url == url, cancellationToken);
  }

  private async Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken)
  {
    return CleanTitle(await titleReader.ReadTitleAsync(url, cancellationToken));
  }

  private static string? CleanTitle(string? title)
  {
    var trimmed = BlankToNull(title);
    if (trimmed is not { Length: > Vacancy.TitleMaxLength }) return trimmed;

    var length = char.IsHighSurrogate(trimmed[Vacancy.TitleMaxLength - 1]) ? Vacancy.TitleMaxLength - 1 : Vacancy.TitleMaxLength;
    return trimmed[..length].TrimEnd();
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record NormalizedVacancy(
    string Url,
    string? Title,
    Guid? CompanyId,
    Guid? RoleId,
    Guid? LocationId,
    List<Guid> PlatformIds,
    List<Guid> EngineIds,
    Guid? LanguageId,
    WorkFormat? WorkFormat,
    SalaryRange? Salary,
    List<Guid> TrackIds,
    string? Description,
    string? Notes);
}
