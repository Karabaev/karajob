using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Hypotheses;

public class HypothesisService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, TimeProvider timeProvider)
{
  public async Task<IReadOnlyList<HypothesisListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var hypotheses = await db.Hypotheses.AsNoTracking().ToListAsync(cancellationToken);

    return hypotheses
      .OrderBy(h => StatusRank(h.Status))
      .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
      .Select(h => new HypothesisListItem(h.Id, h.Name, h.Role, h.WorkFormat, h.Language, h.Status))
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
    var normalized = await ValidateAsync(db, input, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    var hypothesis = new Hypothesis(currentUser.UserId, normalized.Name, normalized.Role, normalized.Language, now);
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
    var normalized = await ValidateAsync(db, input, cancellationToken);
    var now = timeProvider.GetUtcNow().UtcDateTime;

    hypothesis.Name = normalized.Name;
    hypothesis.Role = normalized.Role;
    hypothesis.Language = normalized.Language;
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
      || await db.JobApplications.AnyAsync(a => a.HypothesisId == hypothesisId, cancellationToken);
    if (used)
    {
      throw new ValidationFailedException(new Dictionary<string, string>
      {
        [string.Empty] = "Гипотеза используется в вакансиях или откликах — переведите её в статус Disabled",
      });
    }

    db.Hypotheses.Remove(hypothesis);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<CvOption>> ListCvOptionsAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var documents = await db.CvDocuments.AsNoTracking().ToListAsync(cancellationToken);
    return documents
      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
      .Select(d => new CvOption(d.Id, d.Name, d.Language))
      .ToList();
  }

  private static void Apply(Hypothesis hypothesis, NormalizedHypothesis normalized, HypothesisInput input, DateTime now)
  {
    hypothesis.Locations.Clear();
    hypothesis.Locations.AddRange(normalized.Locations);
    hypothesis.Industries.Clear();
    hypothesis.Industries.AddRange(normalized.Industries);
    hypothesis.WorkFormat = input.WorkFormat;
    hypothesis.Positioning = normalized.Positioning;
    hypothesis.PrimaryCvDocumentId = input.PrimaryCvDocumentId;
    hypothesis.ChangeStatus(input.Status, now);
  }

  private static async Task<NormalizedHypothesis> ValidateAsync(IKarajobDbContext db, HypothesisInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    var name = BlankToNull(input.Name);
    if (name is null)
    {
      errors[nameof(HypothesisInput.Name)] = "Укажите название";
    }

    var role = BlankToNull(input.Role);
    if (role is null)
    {
      errors[nameof(HypothesisInput.Role)] = "Укажите роль";
    }

    var language = BlankToNull(input.Language)?.ToLowerInvariant();
    if (language is null)
    {
      errors[nameof(HypothesisInput.Language)] = "Укажите язык";
    }

    if (input.WorkFormat == WorkFormat.None)
    {
      errors[nameof(HypothesisInput.WorkFormat)] = "Выберите хотя бы один формат";
    }

    if (input.PrimaryCvDocumentId is { } cvId && !await db.CvDocuments.AnyAsync(d => d.Id == cvId, cancellationToken))
    {
      errors[nameof(HypothesisInput.PrimaryCvDocumentId)] = "CV не найдено";
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    return new NormalizedHypothesis(name!, role!, language!, CleanList(input.Locations), CleanList(input.Industries), BlankToNull(input.Positioning));
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
      h.Role,
      h.Locations.ToList(),
      h.Industries.ToList(),
      h.WorkFormat,
      h.Language,
      h.Positioning,
      h.PrimaryCvDocumentId,
      h.Status,
      h.StartedAt);
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record NormalizedHypothesis(
    string Name,
    string Role,
    string Language,
    List<string> Locations,
    List<string> Industries,
    string? Positioning);
}
