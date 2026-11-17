using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Tracks;

public class TrackService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  IStringLocalizer<TrackMessages> localizer)
{
  public async Task<IReadOnlyList<TrackListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var tracks = await db.Tracks.AsNoTracking().ToListAsync(cancellationToken);
    var roleNames = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    var languageCodes = await db.Languages.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);

    var platformNames = await db.Platforms.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
    var platformsByTrack = (await db.TrackPlatforms.AsNoTracking().ToListAsync(cancellationToken))
      .GroupBy(hp => hp.TrackId)
      .ToDictionary(
        g => g.Key,
        g => (IReadOnlyList<string>)g.Select(hp => platformNames[hp.PlatformId]).Order(StringComparer.OrdinalIgnoreCase).ToList());

    return tracks
      .OrderBy(h => StatusRank(h.Status))
      .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
      .Select(h => new TrackListItem(
        h.Id, h.Name, h.RoleId, roleNames[h.RoleId], h.WorkFormat, languageCodes[h.LanguageId], h.Status,
        platformsByTrack.GetValueOrDefault(h.Id, [])))
      .ToList();
  }

  public async Task<TrackDetails?> FindAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var track = await db.Tracks.AsNoTracking().FirstOrDefaultAsync(h => h.Id == trackId, cancellationToken);
    return track is null ? null : ToDetails(track);
  }

  public async Task<TrackDetails> CreateAsync(TrackInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalized = await ValidateAsync(db, input, null, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    var track = new Track(currentUser.UserId, normalized.Name, normalized.RoleId, normalized.LanguageId, now);
    Apply(track, normalized, input, now);

    db.Tracks.Add(track);
    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(track);
  }

  public async Task<TrackDetails> UpdateAsync(Guid trackId, TrackInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var track = await db.Tracks.FirstOrDefaultAsync(h => h.Id == trackId, cancellationToken)
      ?? throw new KeyNotFoundException($"Track {trackId} was not found.");
    var normalized = await ValidateAsync(db, input, track.Status, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    track.Name = normalized.Name;
    track.RoleId = normalized.RoleId;
    track.LanguageId = normalized.LanguageId;
    Apply(track, normalized, input, now);

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(track);
  }

  public async Task DeleteAsync(Guid trackId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var track = await db.Tracks.FirstOrDefaultAsync(h => h.Id == trackId, cancellationToken)
      ?? throw new KeyNotFoundException($"Track {trackId} was not found.");

    var used = await db.VacancyMatches.AnyAsync(m => m.TrackId == trackId, cancellationToken)
      || await db.JobApplications.AnyAsync(a => a.TrackId == trackId, cancellationToken)
      || await db.TrackCheckIns.AnyAsync(c => c.TrackId == trackId, cancellationToken)
      || await db.TrackDecisions.AnyAsync(d => d.TrackId == trackId, cancellationToken);
    if (used)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = localizer["Error.InUse"],
      });
    }

    db.Tracks.Remove(track);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<CvOption>> ListCvOptionsAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var documents = await db.CvDocuments.AsNoTracking().ToListAsync(cancellationToken);
    var languageCodes = await db.Languages.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);
    return documents
      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
      .Select(d => new CvOption(d.Id, d.Name, languageCodes[d.LanguageId]))
      .ToList();
  }

  private static void Apply(Track track, NormalizedTrack normalized, TrackInput input, DateTime now)
  {
    track.SetLocations(input.LocationIds);
    track.SetPlatforms(input.PlatformIds);
    track.SetEngines(input.EngineIds);
    track.Industries.Clear();
    track.Industries.AddRange(normalized.Industries);
    track.WorkFormat = input.WorkFormat;
    track.Positioning = normalized.Positioning;
    track.PrimaryCvDocumentId = input.PrimaryCvDocumentId;
    track.ChangeStatus(input.Status, now);
  }

  private async Task<NormalizedTrack> ValidateAsync(IKarajobDbContext db, TrackInput input, TrackStatus? storedStatus, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    var name = BlankToNull(input.Name);
    if (name is null)
    {
      errors[nameof(TrackInput.Name)] = localizer["Error.NameRequired"];
    }

    var roleId = input.RoleId.GetValueOrDefault();
    if (input.RoleId is null)
    {
      errors[nameof(TrackInput.RoleId)] = localizer["Error.RoleRequired"];
    }
    else if (!await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(TrackInput.RoleId)] = localizer["Error.RoleNotFound"];
    }

    var languageId = input.LanguageId.GetValueOrDefault();
    if (input.LanguageId is null)
    {
      errors[nameof(TrackInput.LanguageId)] = localizer["Error.LanguageRequired"];
    }
    else if (!await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(TrackInput.LanguageId)] = localizer["Error.LanguageNotFound"];
    }

    var locationIds = input.LocationIds.Distinct().ToList();
    if (locationIds.Count > 0 && await db.Locations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken) != locationIds.Count)
    {
      errors[nameof(TrackInput.LocationIds)] = localizer["Error.LocationNotFound"];
    }

    var platformIds = input.PlatformIds.Distinct().ToList();
    if (platformIds.Count > 0 && await db.Platforms.CountAsync(p => platformIds.Contains(p.Id), cancellationToken) != platformIds.Count)
    {
      errors[nameof(TrackInput.PlatformIds)] = localizer["Error.PlatformNotFound"];
    }

    var engineIds = input.EngineIds.Distinct().ToList();
    if (engineIds.Count > 0 && await db.Engines.CountAsync(e => engineIds.Contains(e.Id), cancellationToken) != engineIds.Count)
    {
      errors[nameof(TrackInput.EngineIds)] = localizer["Error.EngineNotFound"];
    }

    if (input.WorkFormat == WorkFormat.None)
    {
      errors[nameof(TrackInput.WorkFormat)] = localizer["Error.WorkFormatRequired"];
    }

    if (input.PrimaryCvDocumentId is { } cvId && !await db.CvDocuments.AnyAsync(d => d.Id == cvId, cancellationToken))
    {
      errors[nameof(TrackInput.PrimaryCvDocumentId)] = localizer["Error.CvNotFound"];
    }

    if (input.Status is TrackStatus.Expanded or TrackStatus.Disabled && input.Status != storedStatus)
    {
      errors[nameof(TrackInput.Status)] = localizer["Error.StatusViaDecision"];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedTrack(name!, roleId, languageId, CleanList(input.Industries), BlankToNull(input.Positioning));
  }

  private static List<string> CleanList(IEnumerable<string> values)
  {
    return values
      .Where(v => !string.IsNullOrWhiteSpace(v))
      .Select(v => v.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static int StatusRank(TrackStatus status)
  {
    return status switch
    {
      TrackStatus.Active => 0,
      TrackStatus.Expanded => 1,
      TrackStatus.Draft => 2,
      TrackStatus.Paused => 3,
      TrackStatus.Disabled => 4,
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
  }

  private static TrackDetails ToDetails(Track h)
  {
    return new TrackDetails(
      h.Id,
      h.Name,
      h.RoleId,
      h.Locations.Select(l => l.LocationId).ToList(),
      h.Platforms.Select(p => p.PlatformId).ToList(),
      h.Engines.Select(e => e.EngineId).ToList(),
      h.Industries.ToList(),
      h.WorkFormat,
      h.LanguageId,
      h.Positioning,
      h.PrimaryCvDocumentId,
      h.Status,
      h.StartedAt,
      h.PeriodStartedAt);
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record NormalizedTrack(
    string Name,
    Guid RoleId,
    Guid LanguageId,
    List<string> Industries,
    string? Positioning);
}
