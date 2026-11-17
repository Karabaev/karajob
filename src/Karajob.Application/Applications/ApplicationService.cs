using Karajob.Application.Persistence;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Applications;

public class ApplicationService(IKarajobDbContextFactory dbContextFactory)
{
  public async Task<IReadOnlyList<ApplicationHistoryItem>> ListHistoryAsync(
    ApplicationHistoryFilter filter,
    CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var query = db.Vacancies.AsNoTracking().Where(v => v.Application != null);
    if (filter.Stage is { } stage)
    {
      query = query.Where(v => v.Stage == stage);
    }

    if (filter.Outcome is { } outcome)
    {
      query = query.Where(v => v.Application!.Outcome == outcome);
    }

    var vacancies = await query
      .Include(v => v.Application)
      .Include(v => v.Events.Where(e => e.Type == ApplicationEventType.StageChanged && e.ToStage == VacancyStage.Closed))
      .ToListAsync(cancellationToken);
    var companyIds = vacancies.Where(v => v.CompanyId is not null).Select(v => v.CompanyId!.Value).Distinct().ToList();
    var roleIds = vacancies.Where(v => v.RoleId is not null).Select(v => v.RoleId!.Value).Distinct().ToList();
    var trackIds = vacancies.Select(v => v.Application!.TrackId).Distinct().ToList();
    var cvVersionIds = vacancies.Select(v => v.Application!.CvVersionId).Distinct().ToList();

    var companies = await db.Companies.AsNoTracking()
      .Where(c => companyIds.Contains(c.Id))
      .ToDictionaryAsync(c => c.Id, c => new { c.Name, c.Slug }, cancellationToken);
    var roleNames = await db.Roles.AsNoTracking()
      .Where(r => roleIds.Contains(r.Id))
      .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    var trackNames = await db.Tracks.AsNoTracking()
      .Where(h => trackIds.Contains(h.Id))
      .ToDictionaryAsync(h => h.Id, h => h.Name, cancellationToken);
    var cvVersions = await db.CvVersions.AsNoTracking()
      .Where(v => cvVersionIds.Contains(v.Id))
      .ToDictionaryAsync(v => v.Id, v => new { v.FileName, v.CvDocumentId }, cancellationToken);
    var documentIds = cvVersions.Values.Select(v => v.CvDocumentId).Distinct().ToList();
    var documentNames = await db.CvDocuments.AsNoTracking()
      .Where(d => documentIds.Contains(d.Id))
      .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

    var search = filter.Search?.Trim();
    if (!string.IsNullOrEmpty(search))
    {
      vacancies = vacancies
        .Where(v =>
          v.CompanyId is { } companyId && (Contains(companies[companyId].Name, search) || Contains(companies[companyId].Slug, search))
          || v.RoleId is { } roleId && Contains(roleNames[roleId], search)
          || Contains(v.Url, search))
        .ToList();
    }

    return vacancies
      .OrderByDescending(v => v.Application!.AppliedAt)
      .Select(v =>
      {
        var application = v.Application!;
        var cvVersion = cvVersions[application.CvVersionId];
        return new ApplicationHistoryItem(
          v.Id,
          v.Url,
          v.CompanyId is { } companyId ? companies[companyId].Name : null,
          v.RoleId is { } roleId ? roleNames[roleId] : null,
          v.Stage,
          application.Outcome,
          application.TrackId,
          trackNames[application.TrackId],
          documentNames[cvVersion.CvDocumentId],
          cvVersion.FileName,
          application.Channel,
          application.AppliedAt,
          v.ClosedAt,
          application.CoverLetter,
          v.ClosedFromStage,
          v.ReopenTarget);
      })
      .ToList();
  }

  private static bool Contains(string value, string search)
  {
    return value.Contains(search, StringComparison.OrdinalIgnoreCase);
  }
}
