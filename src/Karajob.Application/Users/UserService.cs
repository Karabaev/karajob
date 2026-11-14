using Karajob.Application.Persistence;

namespace Karajob.Application.Users;

public class UserService(IKarajobDbContextFactory dbContextFactory, TimeProvider timeProvider)
{
  public Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<UserDto> CreateAsync(UserInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<UserDto> UpdateAsync(Guid id, UserInput input, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
