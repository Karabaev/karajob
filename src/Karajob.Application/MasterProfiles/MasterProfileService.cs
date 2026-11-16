using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Domain.MasterProfiles;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.MasterProfiles;

public class MasterProfileService(IKarajobDbContextFactory dbContextFactory, ICurrentUser currentUser, TimeProvider timeProvider)
{
  public async Task<MasterProfileDetails> GetAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var profile = await LoadOrCreateAsync(db, cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(profile);
  }

  public async Task<MasterProfileDetails> SaveAsync(MasterProfileInput input, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(input.Markdown))
    {
      throw MarkdownError("Профиль не может быть пустым");
    }

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var profile = await LoadOrCreateAsync(db, cancellationToken);
    if (profile.CurrentVersion.Markdown == input.Markdown)
    {
      throw MarkdownError("Нет изменений");
    }

    profile.AddVersion(input.Markdown, input.Comment, Now());
    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(profile);
  }

  public async Task<MasterProfileVersionDto> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var version = await db.MasterProfileVersions.AsNoTracking().SingleOrDefaultAsync(v => v.Id == versionId, cancellationToken)
      ?? throw new KeyNotFoundException($"Master profile version {versionId} was not found.");
    return new MasterProfileVersionDto(version.Id, version.Markdown, version.CreatedAt, version.Comment, version.RestoredFromVersionId);
  }

  public async Task<MasterProfileDetails> RestoreAsync(Guid versionId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var profile = await db.MasterProfiles.SingleOrDefaultAsync(cancellationToken);
    var source = profile?.Versions.SingleOrDefault(v => v.Id == versionId)
      ?? throw new KeyNotFoundException($"Master profile version {versionId} was not found.");
    if (profile.CurrentVersion.Markdown == source.Markdown)
    {
      throw MarkdownError("Это уже текущая версия");
    }

    profile.AddVersion(source.Markdown, null, Now(), source.Id);
    await db.SaveChangesAsync(cancellationToken);
    return ToDetails(profile);
  }

  private async Task<MasterProfile> LoadOrCreateAsync(IKarajobDbContext db, CancellationToken cancellationToken)
  {
    var profile = await db.MasterProfiles.SingleOrDefaultAsync(cancellationToken);
    if (profile is not null)
    {
      return profile;
    }

    profile = new MasterProfile(currentUser.UserId, MasterProfileTemplate.Markdown, Now());
    db.MasterProfiles.Add(profile);
    return profile;
  }

  private DateTime Now()
  {
    return timeProvider.GetUtcNow().UtcDateTime;
  }

  private static ValidationFailedException MarkdownError(string message)
  {
    return new ValidationFailedException(new Dictionary<string, string> { [nameof(MasterProfileInput.Markdown)] = message });
  }

  private static MasterProfileDetails ToDetails(MasterProfile profile)
  {
    var current = profile.CurrentVersion;
    var versions = profile.NewestFirst()
      .Select(v => new MasterProfileVersionSummary(v.Id, v.CreatedAt, v.Comment, v.RestoredFromVersionId))
      .ToList();
    return new MasterProfileDetails(current.Markdown, current.Id, versions);
  }
}
