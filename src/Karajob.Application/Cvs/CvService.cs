using Karajob.Application.Files;
using Karajob.Application.Persistence;
using Karajob.Application.Users;

namespace Karajob.Application.Cvs;

public class CvService(
  IKarajobDbContextFactory dbContextFactory,
  ICurrentUser currentUser,
  IFileStorage fileStorage,
  TimeProvider timeProvider,
  CvOptions options)
{
  public const string FileErrorKey = "File";

  public Task<IReadOnlyList<CvListItem>> ListAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CvDetails?> FindAsync(Guid cvDocumentId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CvDetails> CreateAsync(CvInput input, CvUpload upload, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CvDetails> UpdateAsync(Guid cvDocumentId, CvInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CvDetails> AddVersionAsync(Guid cvDocumentId, CvUpload upload, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteVersionAsync(Guid cvVersionId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteAsync(Guid cvDocumentId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<CvFile?> OpenVersionAsync(Guid cvVersionId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
