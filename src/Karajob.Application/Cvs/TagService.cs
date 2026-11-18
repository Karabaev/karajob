using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Resources;
using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Karajob.Application.Cvs;

public class TagService(IKarajobDbContextFactory dbContextFactory, IStringLocalizer<CvMessages> localizer)
{
  public const string ColorErrorKey = "Color";

  public async Task<IReadOnlyList<TagListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var counts = await db.CvDocumentTags.AsNoTracking()
      .GroupBy(t => t.TagId)
      .Select(g => new { TagId = g.Key, Count = g.Count() })
      .ToDictionaryAsync(g => g.TagId, g => g.Count, cancellationToken);
    var tags = await db.Tags.AsNoTracking().ToListAsync(cancellationToken);

    return tags
      .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
      .Select(t => new TagListItem(t.Id, t.Name, t.Color, counts.GetValueOrDefault(t.Id)))
      .ToList();
  }

  public async Task<IReadOnlyList<TagItem>> SearchAsync(string? prefix, int limit, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalized = string.IsNullOrWhiteSpace(prefix) ? "" : prefix.Trim().ToLowerInvariant();
    var tags = await db.Tags.AsNoTracking()
      .Where(t => t.Name.StartsWith(normalized))
      .ToListAsync(cancellationToken);

    return tags
      .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
      .Take(limit)
      .Select(t => new TagItem(t.Id, t.Name, t.Color))
      .ToList();
  }

  public async Task SetColorAsync(Guid tagId, string? color, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken)
      ?? throw new KeyNotFoundException($"Tag {tagId} was not found.");

    try
    {
      tag.SetColor(color);
    }
    catch (ArgumentException)
    {
      throw new ValidationFailedException(new Dictionary<string, string> { [ColorErrorKey] = localizer["Error.ColorInvalid"] });
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid tagId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken)
      ?? throw new KeyNotFoundException($"Tag {tagId} was not found.");

    db.Tags.Remove(tag);
    await db.SaveChangesAsync(cancellationToken);
  }
}
