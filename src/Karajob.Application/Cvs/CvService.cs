using Karajob.Application.Common;
using Karajob.Application.Files;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Cvs;

public class CvService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  IFileStorage fileStorage,
  TimeProvider timeProvider,
  CvOptions options)
{
  public const string FileErrorKey = "File";

  private const string PdfContentType = "application/pdf";
  private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

  public async Task<IReadOnlyList<CvListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var documents = await db.CvDocuments.AsNoTracking().Include(d => d.Versions).ToListAsync(cancellationToken);
    var languageCodes = await db.Languages.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);

    return documents
      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
      .Select(d => new CvListItem(
        d.Id,
        d.Name,
        languageCodes[d.LanguageId],
        d.Tags.ToList(),
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
    return document is null ? null : ToDetails(document);
  }

  public async Task<CvDetails> CreateAsync(CvInput input, CvUpload upload, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var errors = await ValidateInputAsync(db, input, cancellationToken);
    var file = ValidateUpload(upload, errors);
    ThrowIfAny(errors);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var document = new CvDocument(currentUser.UserId, input.Name.Trim(), input.LanguageId!.Value, now);
    document.Tags.AddRange(CleanList(input.Tags));

    var storagePath = await StoreAsync(upload, file!, cancellationToken);
    document.AddVersion(file!.FileName, storagePath, file.ContentType, upload.SizeBytes, BlankToNull(upload.Comment), now);
    db.CvDocuments.Add(document);
    await SaveOrDiscardFileAsync(db, [storagePath], cancellationToken);
    return ToDetails(document);
  }

  public async Task<CvDetails> UpdateAsync(Guid cvDocumentId, CvInput input, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");
    ThrowIfAny(await ValidateInputAsync(db, input, cancellationToken));

    document.Name = input.Name.Trim();
    document.LanguageId = input.LanguageId!.Value;
    document.Tags.Clear();
    document.Tags.AddRange(CleanList(input.Tags));

    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(document);
  }

  public async Task<CvDetails> AddVersionAsync(Guid cvDocumentId, CvUpload upload, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");
    var errors = new Dictionary<string, string>();
    var file = ValidateUpload(upload, errors);
    ThrowIfAny(errors);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var storagePath = await StoreAsync(upload, file!, cancellationToken);
    document.AddVersion(file!.FileName, storagePath, file.ContentType, upload.SizeBytes, BlankToNull(upload.Comment), now);
    await SaveOrDiscardFileAsync(db, [storagePath], cancellationToken);
    return ToDetails(document);
  }

  public async Task DeleteVersionAsync(Guid cvVersionId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var version = await db.CvVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == cvVersionId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV version {cvVersionId} was not found.");

    if (await db.JobApplications.AnyAsync(a => a.CvVersionId == cvVersionId, cancellationToken))
    {
      throw Blocked("Версия используется в откликах — её нельзя удалить");
    }

    var document = await db.CvDocuments.Include(d => d.Versions).FirstAsync(d => d.Id == version.CvDocumentId, cancellationToken);
    if (document.Versions.Count == 1)
    {
      throw Blocked("Это единственная версия — удалите CV целиком");
    }

    document.RemoveVersion(cvVersionId);
    await db.SaveChangesAsync(cancellationToken);
    fileStorage.Delete(version.StoragePath);
  }

  public async Task DeleteAsync(Guid cvDocumentId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var document = await db.CvDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == cvDocumentId, cancellationToken)
      ?? throw new KeyNotFoundException($"CV {cvDocumentId} was not found.");

    var versionIds = document.Versions.Select(v => v.Id).ToList();
    if (await db.JobApplications.AnyAsync(a => versionIds.Contains(a.CvVersionId), cancellationToken))
    {
      throw Blocked("CV используется в откликах — его нельзя удалить");
    }

    if (await db.Hypotheses.AnyAsync(h => h.PrimaryCvDocumentId == cvDocumentId, cancellationToken))
    {
      throw Blocked("CV выбрано основным в гипотезе — сначала смените его там");
    }

    var paths = document.Versions.Select(v => v.StoragePath).ToList();
    db.CvDocuments.Remove(document);
    await db.SaveChangesAsync(cancellationToken);
    foreach (var path in paths)
    {
      fileStorage.Delete(path);
    }
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

  private async Task<Dictionary<string, string>> ValidateInputAsync(IKarajobDbContext db, CvInput input, CancellationToken cancellationToken)
  {
    var errors = new Dictionary<string, string>();

    if (BlankToNull(input.Name) is null)
    {
      errors[nameof(CvInput.Name)] = "Укажите название";
    }

    var languageId = input.LanguageId.GetValueOrDefault();
    if (input.LanguageId is null)
    {
      errors[nameof(CvInput.LanguageId)] = "Укажите язык";
    }
    else if (!await db.Languages.AnyAsync(l => l.Id == languageId, cancellationToken))
    {
      errors[nameof(CvInput.LanguageId)] = "Язык не найден";
    }

    return errors;
  }

  private ValidatedFile? ValidateUpload(CvUpload upload, Dictionary<string, string> errors)
  {
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
      errors[FileErrorKey] = "Только PDF или DOCX";
    }
    else if (upload.SizeBytes <= 0)
    {
      errors[FileErrorKey] = "Файл пустой";
    }
    else if (upload.SizeBytes > options.MaxFileSizeBytes)
    {
      errors[FileErrorKey] = $"Файл больше {options.MaxFileSizeMb} МБ";
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

  private static CvDetails ToDetails(CvDocument document)
  {
    return new CvDetails(
      document.Id,
      document.Name,
      document.LanguageId,
      document.Tags.ToList(),
      document.Versions
        .OrderByDescending(v => v.CreatedAt)
        .ThenByDescending(v => v.Id)
        .Select(v => new CvVersionItem(v.Id, v.FileName, v.SizeBytes, v.Comment, v.CreatedAt))
        .ToList());
  }

  private static List<string> CleanList(IEnumerable<string> values)
  {
    return values
      .Where(v => !string.IsNullOrWhiteSpace(v))
      .Select(v => v.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static string? BlankToNull(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private record ValidatedFile(string FileName, string Extension, string ContentType);
}
