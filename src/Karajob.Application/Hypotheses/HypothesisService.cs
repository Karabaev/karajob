using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Hypotheses;

public class HypothesisService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  TimeProvider timeProvider,
  IStringLocalizer<HypothesisMessages> localizer)
{
  public async Task<IReadOnlyList<HypothesisListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypotheses = await db.Hypotheses.AsNoTracking().ToListAsync(cancellationToken);
    var roleNames = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    var languageCodes = await db.Languages.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);

    var platformNames = await db.Platforms.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
    var platformsByHypothesis = (await db.HypothesisPlatforms.AsNoTracking().ToListAsync(cancellationToken))
      .GroupBy(hp => hp.HypothesisId)
      .ToDictionary(
        g => g.Key,
        g => (IReadOnlyList<string>)g.Select(hp => platformNames[hp.PlatformId]).Order(StringComparer.OrdinalIgnoreCase).ToList());

    return hypotheses
      .OrderBy(h => StatusRank(h.Status))
      .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
      .Select(h => new HypothesisListItem(
        h.Id, h.Name, h.RoleId, roleNames[h.RoleId], h.WorkFormat, languageCodes[h.LanguageId], h.Status,
        platformsByHypothesis.GetValueOrDefault(h.Id, [])))
      .ToList();
  }

  public async Task<HypothesisDetails?> FindAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hypothesisId, cancellationToken);
    return hypothesis is null ? null : ToDetails(hypothesis);
  }

  public async Task<HypothesisDetails> CreateAsync(HypothesisInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalized = await ValidateAsync(db, input, null, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    var hypothesis = new Hypothesis(currentUser.UserId, normalized.Name, normalized.RoleId, normalized.LanguageId, now);
    Apply(hypothesis, normalized, input, now);

    db.Hypotheses.Add(hypothesis);
    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(hypothesis);
  }

  public async Task<HypothesisDetails> UpdateAsync(Guid hypothesisId, HypothesisInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.FirstOrDefaultAsync(h => h.Id == hypothesisId, cancellationToken)
      ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");
    var normalized = await ValidateAsync(db, input, hypothesis.Status, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    hypothesis.Name = normalized.Name;
    hypothesis.RoleId = normalized.RoleId;
    hypothesis.LanguageId = normalized.LanguageId;
    Apply(hypothesis, normalized, input, now);

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(hypothesis);
  }

  public async Task DeleteAsync(Guid hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypothesis = await db.Hypotheses.FirstOrDefaultAsync(h => h.Id == hypothesisId, cancellationToken)
      ?? throw new KeyNotFoundException($"Hypothesis {hypothesisId} was not found.");

    var used = await db.VacancyMatches.AnyAsync(m => m.HypothesisId == hypothesisId, cancellationToken)
      || await db.JobApplications.AnyAsync(a => a.HypothesisId == hypothesisId, cancellationToken)
      || await db.HypothesisCheckIns.AnyAsync(c => c.HypothesisId == hypothesisId, cancellationToken)
      || await db.HypothesisDecisions.AnyAsync(d => d.HypothesisId == hypothesisId, cancellationToken);
    if (used)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = localizer["Error.InUse"],
      });
    }

    db.Hypotheses.Remove(hypothesis);
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

  private static void Apply(Hypothesis hypothesis, NormalizedHypothesis normalized, HypothesisInput input, DateTime now)
  {
    hypothesis.SetLocations(input.LocationIds);
    hypothesis.SetPlatforms(input.PlatformIds);
    hypothesis.SetEngines(input.EngineIds);
    hypothesis.Industries.Clear();
    hypothesis.Industries.AddRange(normalized.Industries);
    hypothesis.WorkFormat = input.WorkFormat;
    hypothesis.Positioning = normalized.Positioning;
    hypothesis.PrimaryCvDocumentId = input.PrimaryCvDocumentId;
    hypothesis.ChangeStatus(input.Status, now);
  }

  private async Task<NormalizedHypothesis> ValidateAsync(IKarajobDbContext db, HypothesisInput input, HypothesisStatus? storedStatus, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    var name = BlankToNull(input.Name);
    if (name is null)
    {
      errors[nameof(HypothesisInput.Name)] = localizer["Error.NameRequired"];
    }

    var roleId = input.RoleId.GetValueOrDefault();
    if (input.RoleId is null)
    {
      errors[nameof(HypothesisInput.RoleId)] = localizer["Error.RoleRequired"];
    }
    else if (!await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
    {
      errors[nameof(HypothesisInput.RoleId)] = localizer["Error.RoleNotFound"];
    }

    var languageId = input.LanguageId.GetValueOrDefault();
    if (input.LanguageId is null)
    {
      errors[nameof(HypothesisInput.LanguageId)] = localizer["Error.LanguageRequired"];
    }
    else if (!await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(HypothesisInput.LanguageId)] = localizer["Error.LanguageNotFound"];
    }

    var locationIds = input.LocationIds.Distinct().ToList();
    if (locationIds.Count > 0 && await db.Locations.CountAsync(l => locationIds.Contains(l.Id), cancellationToken) != locationIds.Count)
    {
      errors[nameof(HypothesisInput.LocationIds)] = localizer["Error.LocationNotFound"];
    }

    var platformIds = input.PlatformIds.Distinct().ToList();
    if (platformIds.Count > 0 && await db.Platforms.CountAsync(p => platformIds.Contains(p.Id), cancellationToken) != platformIds.Count)
    {
      errors[nameof(HypothesisInput.PlatformIds)] = localizer["Error.PlatformNotFound"];
    }

    var engineIds = input.EngineIds.Distinct().ToList();
    if (engineIds.Count > 0 && await db.Engines.CountAsync(e => engineIds.Contains(e.Id), cancellationToken) != engineIds.Count)
    {
      errors[nameof(HypothesisInput.EngineIds)] = localizer["Error.EngineNotFound"];
    }

    if (input.WorkFormat == WorkFormat.None)
    {
      errors[nameof(HypothesisInput.WorkFormat)] = localizer["Error.WorkFormatRequired"];
    }

    if (input.PrimaryCvDocumentId is { } cvId && !await db.CvDocuments.AnyAsync(d => d.Id == cvId, cancellationToken))
    {
      errors[nameof(HypothesisInput.PrimaryCvDocumentId)] = localizer["Error.CvNotFound"];
    }

    if (input.Status is HypothesisStatus.Expanded or HypothesisStatus.Disabled && input.Status != storedStatus)
    {
      errors[nameof(HypothesisInput.Status)] = localizer["Error.StatusViaDecision"];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedHypothesis(name!, roleId, languageId, CleanList(input.Industries), BlankToNull(input.Positioning));
  }

  private static List<string> CleanList(IEnumerable<string> values)
  {
    return values
      .Where(v => !string.IsNullOrWhiteSpace(v))
      .Select(v => v.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static int StatusRank(HypothesisStatus status)
  {
    return status switch
    {
      HypothesisStatus.Active => 0,
      HypothesisStatus.Expanded => 1,
      HypothesisStatus.Draft => 2,
      HypothesisStatus.Paused => 3,
      HypothesisStatus.Disabled => 4,
      _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
  }

  private static HypothesisDetails ToDetails(Hypothesis h)
  {
    return new HypothesisDetails(
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

  private record NormalizedHypothesis(
    string Name,
    Guid RoleId,
    Guid LanguageId,
    List<string> Industries,
    string? Positioning);
}
