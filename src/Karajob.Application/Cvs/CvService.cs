using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Files;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karajob.Application.Cvs;

public class CvService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  IFileStorage fileStorage,
  TimeProvider timeProvider,
  CvOptions options,
  ILogger<CvService> logger,
  IStringLocalizer<CvMessages> localizer)
{
  public const string FileErrorKey = "File";

  private const string PdfContentType = "application/pdf";
  private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

  public async Task<IReadOnlyList<CvListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var documents = await db.CvDocuments.AsNoTracking().Include(d => d.Versions).ToListAsync(cancellationToken);
    var languageCodes = await db.Languages.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);
    var tagItems = await LoadTagItemsAsync(db, cancellationToken);

    return documents
      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
      .Select(d => new CvListItem(
        d.Id,
        d.Name,
        languageCodes[d.LanguageId],
        ToTagItems(d.Tags, tagItems),
        d.Versions.Count,
        d.Versions.Max(v => v.CreatedAt)))
      .ToList();
  }

  public async Task<CvDetails?> FindAsync(Guid cvDocumentId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.AsNoTracking()
      .Include(d => d.Versions)
      .FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken);
    return document is null ? null : ToDetails(document, await LoadTagItemsAsync(db, cancellationToken));
  }

  public async Task<CvDetails> CreateAsync(CvInput input, CvUpload? upload, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var errors = await ValidateInputAsync(db, input, cancellationToken);
    var file = ValidateUpload(upload, errors);
    ThrowIfAny(errors);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var document = new CvDocument(currentUser.UserId, input.Name.Trim(), input.LanguageId!.Value, now);
    document.SetTags((await ResolveTagsAsync(db, input.Tags, cancellationToken)).Select(t => t.Id));

    var storagePath = await StoreAsync(upload!, file!, cancellationToken);
    document.AddVersion(file!.FileName, storagePath, file.ContentType, upload!.SizeBytes, BlankToNull(upload.Comment), now);
    db.CvDocuments.Add(document);
    await SaveOrDiscardFileAsync(db, [storagePath], cancellationToken);
    return ToDetails(document, await LoadTagItemsAsync(db, cancellationToken));
  }

  public async Task<CvDetails> UpdateAsync(Guid cvDocumentId, CvInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");
    ThrowIfAny(await ValidateInputAsync(db, input, cancellationToken));

    document.Name = input.Name.Trim();
    document.LanguageId = input.LanguageId!.Value;
    document.SetTags((await ResolveTagsAsync(db, input.Tags, cancellationToken)).Select(t => t.Id));

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(document, await LoadTagItemsAsync(db, cancellationToken));
  }

  public async Task<CvDetails> AddVersionAsync(Guid cvDocumentId, CvUpload? upload, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");
    var errors = new Dictionary<string, string>();
    var file = ValidateUpload(upload, errors);
    ThrowIfAny(errors);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var storagePath = await StoreAsync(upload!, file!, cancellationToken);
    document.AddVersion(file!.FileName, storagePath, file.ContentType, upload!.SizeBytes, BlankToNull(upload.Comment), now);
    await SaveOrDiscardFileAsync(db, [storagePath], cancellationToken);
    return ToDetails(document, await LoadTagItemsAsync(db, cancellationToken));
  }

  public async Task DeleteVersionAsync(Guid cvVersionId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var version = await db.CvVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == cvVersionId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV version {cvVersionId} was not found.");

    if (await db.JobApplications.AnyAsync(a => a.CvVersionId == cvVersionId, cancellationToken))
    {
      throw Blocked(localizer["Error.VersionInUse"]);
    }

    var document = await db.CvDocuments.Include(d => d.Versions).FirstAsync(d => d.Id == version.CvDocumentId, cancellationToken);
    if (document.Versions.Count == 1)
    {
      throw Blocked(localizer["Error.LastVersion"]);
    }

    document.RemoveVersion(cvVersionId);
    await db.SaveChangesAsync(cancellationToken);
    DeleteCommittedFiles([version.StoragePath]);
  }

  public async Task DeleteAsync(Guid cvDocumentId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");

    var versionIds = document.Versions.Select(v => v.Id).ToList();
    if (await db.JobApplications.AnyAsync(a => versionIds.Contains(a.CvVersionId), cancellationToken))
    {
      throw Blocked(localizer["Error.CvInUse"]);
    }

    if (await db.Tracks.AnyAsync(h => h.PrimaryCvDocumentId == cvDocumentId, cancellationToken))
    {
      throw Blocked(localizer["Error.CvPrimaryInTrack"]);
    }

    var paths = document.Versions.Select(v => v.StoragePath).ToList();
    db.CvDocuments.Remove(document);
    await db.SaveChangesAsync(cancellationToken);
    DeleteCommittedFiles(paths);
  }

  public async Task<CvFile?> OpenVersionAsync(Guid cvVersionId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var version = await db.CvVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == cvVersionId, cancellationToken);
    return version is null ? null : new CvFile(fileStorage.OpenRead(version.StoragePath), version.FileName, version.ContentType);
  }

  private async Task<string> StoreAsync(CvUpload upload, ValidatedFile file, CancellationToken cancellationToken)
  {
    var storagePath = $"cv/{currentUser.UserId:N}/{Guid.CreateVersion7():N}{file.Extension}";
    await using var stream = upload.OpenRead();
    await fileStorage.SaveAsync(storagePath, stream, cancellationToken);
    return storagePath;
  }

  private async Task SaveOrDiscardFileAsync(IKarajobDbContext db, IReadOnlyList<string> storedPaths, CancellationToken cancellationToken)
  {
    try
    {
      await db.SaveChangesAsync(cancellationToken);
    }
    catch
    {
      foreach (var path in storedPaths)
      {
        fileStorage.Delete(path);
      }

      throw;
    }
  }

  // The rows are already gone, so a file left behind is only wasted space — not a reason to report the delete as failed.
  private void DeleteCommittedFiles(IEnumerable<string> paths)
  {
    foreach (var path in paths)
    {
      try
      {
        fileStorage.Delete(path);
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
      {
        logger.LogWarning(exception, "Could not delete stored file {Path} of a deleted CV version", path);
      }
    }
  }

  private async Task<Dictionary<string, string>> ValidateInputAsync(IKarajobDbContext db, CvInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    if (BlankToNull(input.Name) is null)
    {
      errors[nameof(CvInput.Name)] = localizer["Error.NameRequired"];
    }

    var languageId = input.LanguageId.GetValueOrDefault();
    if (input.LanguageId is null)
    {
      errors[nameof(CvInput.LanguageId)] = localizer["Error.LanguageRequired"];
    }
    else if (!await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(CvInput.LanguageId)] = localizer["Error.LanguageNotFound"];
    }

    if (CleanTagNames(input.Tags).Any(n => n.Length > Tag.MaxNameLength))
    {
      errors[nameof(CvInput.Tags)] = localizer["Error.TagTooLong", Tag.MaxNameLength];
    }

    return errors;
  }

  private ValidatedFile? ValidateUpload(CvUpload? upload, Dictionary<string, string> errors)
  {
    if (upload is null)
    {
      errors[FileErrorKey] = localizer["Error.FileRequired"];
      return null;
    }

    var fileName = upload.FileName.Split('\\', '/')[^1];
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    var contentType = extension switch
    {
      ".pdf" => PdfContentType,
      ".docx" => DocxContentType,
      _ => null,
    };

    if (contentType is null)
    {
      errors[FileErrorKey] = localizer["Error.FileTypeInvalid"];
    }
    else if (upload.SizeBytes <= 0)
    {
      errors[FileErrorKey] = localizer["Error.FileEmpty"];
    }
    else if (upload.SizeBytes > options.MaxFileSizeBytes)
    {
      errors[FileErrorKey] = localizer["Error.FileTooLarge", options.MaxFileSizeMb];
    }

    return contentType is null ? null : new ValidatedFile(fileName, extension, contentType);
  }

  private static void ThrowIfAny(Dictionary<string, string> errors)
  {
    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }
  }

  private static ValidationFailedException Blocked(string message)
  {
    return new ValidationFailedException(new Dictionary<string, string> { [string.Empty] = message });
  }

  private static CvDetails ToDetails(CvDocument document, IReadOnlyDictionary<Guid, TagItem> tagItems)
  {
    return new CvDetails(
      document.Id,
      document.Name,
      document.LanguageId,
      ToTagItems(document.Tags, tagItems),
      document.Versions
        .OrderByDescending(v => v.CreatedAt)
        .ThenByDescending(v => v.Id)
        .Select(v => new CvVersionItem(v.Id, v.FileName, v.SizeBytes, v.Comment, v.CreatedAt))
        .ToList());
  }
  private static async Task<Dictionary<Guid, TagItem>> LoadTagItemsAsync(IKarajobDbContext db, CancellationToken cancellationToken)
  {
    var tags = await db.Tags.AsNoTracking().ToListAsync(cancellationToken);
    return tags.ToDictionary(t => t.Id, t => new TagItem(t.Id, t.Name, t.Color));
  }

  private static IReadOnlyList<TagItem> ToTagItems(IEnumerable<CvDocumentTag> links, IReadOnlyDictionary<Guid, TagItem> tagItems)
  {
    return links
      .Select(l => tagItems[l.TagId])
      .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private async Task<List<Tag>> ResolveTagsAsync(IKarajobDbContext db, IEnumerable<string> names, CancellationToken cancellationToken)
  {
    var wanted = CleanTagNames(names);
    var existing = await db.Tags.Where(t => wanted.Contains(t.Name)).ToListAsync(cancellationToken);
    var created = wanted
      .Except(existing.Select(t => t.Name))
      .Select(name => new Tag(currentUser.UserId, name))
      .ToList();
    db.Tags.AddRange(created);
    return [.. existing, .. created];
  }

  private static List<string> CleanTagNames(IEnumerable<string> names)
  {
    return names
      .Where(n => !string.IsNullOrWhiteSpace(n))
      .Select(n => n.Trim().ToLowerInvariant())
      .Distinct()
      .ToList();
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record ValidatedFile(string FileName, string Extension, string ContentType);
}
