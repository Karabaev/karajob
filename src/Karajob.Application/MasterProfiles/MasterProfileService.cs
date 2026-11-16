using Karajob.Application.Persistence;

namespace Karajob.Application.MasterProfiles;

public class MasterProfileService(IKarajobDbContextFactory dbContextFactory, TimeProvider timeProvider)
{
  public Task<MasterProfileDetails> GetAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<MasterProfileDetails> SaveAsync(MasterProfileInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<MasterProfileVersionDto> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<MasterProfileDetails> RestoreAsync(Guid versionId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
