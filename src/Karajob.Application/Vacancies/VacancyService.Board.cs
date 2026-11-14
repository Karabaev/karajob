using Karajob.Application.Common;
using Karajob.Domain.Applications;
using Karajob.Domain.Common;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Vacancies;

public partial class VacancyService
{
  public async Task<BoardView> GetBoardAsync(Guid? hypothesisId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var columns = BoardMoves.Columns;
    var query = db.Vacancies.AsNoTracking().Where(v => columns.Contains(v.Stage));
    if (hypothesisId is { } id)
    {
      query = query.Where(v => v.Matches.Any(m => m.HypothesisId == id));
    }

    var vacancies = await query.Include(v => v.Application).ToListAsync(cancellationToken);
    var companyIds = vacancies.Where(v => v.CompanyId is not null).Select(v => v.CompanyId!.Value).Distinct().ToList();
    var roleIds = vacancies.Where(v => v.RoleId is not null).Select(v => v.RoleId!.Value).Distinct().ToList();
    var companyNames = await db.Companies.AsNoTracking()
      .Where(c => companyIds.Contains(c.Id))
      .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    var roleNames = await db.Roles.AsNoTracking()
      .Where(r => roleIds.Contains(r.Id))
      .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

    var cards = vacancies
      .OrderByDescending(v => v.DiscoveredAt)
      .Select(v => new BoardCard(
        v.Id,
        v.Url,
        v.CompanyId is { } companyId ? companyNames[companyId] : null,
        v.RoleId is { } roleId ? roleNames[roleId] : null,
        v.Stage,
        v.Application?.Outcome,
        v.DiscoveredAt))
      .ToList();

    var closedOutcomes = Enum.GetValues<ApplicationOutcome>()
      .ToDictionary(o => o, o => cards.Count(c => c.Stage == VacancyStage.Closed && c.Outcome == o));
    return new BoardView(cards, closedOutcomes);
  }

  public async Task MoveAsync(Guid vacancyId, VacancyStage expectedStage, VacancyStage target, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    EnsureStage(vacancy, expectedStage);

    var kind = BoardMoves.Classify(vacancy.Stage, target);
    if (kind != BoardMoveKind.Direct)
    {
      throw Failure(kind is BoardMoveKind.Apply or BoardMoveKind.Close ? "Для этого перехода нужен диалог" : "Такой переход недопустим");
    }

    try
    {
      vacancy.MoveTo(target, timeProvider.GetUtcNow().UtcDateTime);
    }
    catch (StageTransitionException)
    {
      throw Failure("Такой переход недопустим");
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<ApplyOptions> GetApplyOptionsAsync(Guid vacancyId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    if (!await db.Vacancies.AnyAsync(v => v.Id == vacancyId, cancellationToken))
    {
      throw new KeyNotFoundException($"Vacancy {vacancyId} was not found.");
    }

    var matchedIds = await db.VacancyMatches.AsNoTracking()
      .Where(m => m.VacancyId == vacancyId)
      .Select(m => m.HypothesisId)
      .ToListAsync(cancellationToken);

    var hypotheses = await db.Hypotheses.AsNoTracking()
      .Where(h => matchedIds.Contains(h.Id))
      .OrderBy(h => h.Name)
      .Select(h => new { h.Id, h.Name, h.PrimaryCvDocumentId })
      .ToListAsync(cancellationToken);
    var documents = await db.CvDocuments.AsNoTracking().Include(d => d.Versions).ToListAsync(cancellationToken);

    var versions = documents
      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
      .SelectMany(d => d.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new CvVersionOption(v.Id, d.Name, v.FileName, v.CreatedAt)))
      .ToList();
    var options = hypotheses
      .Select(h => new ApplyHypothesisOption(
        h.Id,
        h.Name,
        documents.FirstOrDefault(d => d.Id == h.PrimaryCvDocumentId)?.Versions.MaxBy(v => v.CreatedAt)?.Id))
      .ToList();
    return new ApplyOptions(options, versions);
  }

  public async Task ApplyAsync(Guid vacancyId, VacancyStage expectedStage, ApplyInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    EnsureStage(vacancy, expectedStage);

    var errors = new Dictionary<string, string>();
    if (vacancy.Matches.All(m => m.HypothesisId != input.HypothesisId))
    {
      errors[nameof(ApplyInput.HypothesisId)] = "Гипотеза не подходит к вакансии";
    }

    if (!await db.CvVersions.AnyAsync(v => v.Id == input.CvVersionId, cancellationToken))
    {
      errors[nameof(ApplyInput.CvVersionId)] = "Версия CV не найдена";
    }

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var appliedAt = UtcGuard.Ensure(input.AppliedAt);
    var last = vacancy.Events.Where(e => e.Type == ApplicationEventType.StageChanged).Max(e => e.OccurredAt);
    if (appliedAt > now)
    {
      errors[nameof(ApplyInput.AppliedAt)] = "Дата отклика в будущем";
    }
    else if (appliedAt.Date < last.Date)
    {
      errors[nameof(ApplyInput.AppliedAt)] = "Дата отклика раньше последней смены стадии";
    }
    else if (appliedAt < last)
    {
      // The dialog picks a day, not a time; moving to the last transition keeps the timeline ordered.
      appliedAt = last;
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }

    try
    {
      vacancy.Apply(input.HypothesisId, input.CvVersionId, input.Channel, appliedAt, BlankToNull(input.CoverLetter));
    }
    catch (StageTransitionException)
    {
      throw Failure("Откликнуться на вакансию на этой стадии нельзя");
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task CloseAsync(Guid vacancyId, VacancyStage expectedStage, ApplicationOutcome outcome, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var vacancy = await FindVacancyAsync(db, vacancyId, cancellationToken);
    EnsureStage(vacancy, expectedStage);

    try
    {
      vacancy.Close(outcome, timeProvider.GetUtcNow().UtcDateTime);
    }
    catch (StageTransitionException)
    {
      throw Failure("Вакансию на этой стадии нельзя закрыть");
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  private static ValidationFailedException Failure(string message)
  {
    return new ValidationFailedException(new Dictionary<string, string> { [string.Empty] = message });
  }
}
