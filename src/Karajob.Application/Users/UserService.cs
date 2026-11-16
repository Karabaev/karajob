using Karajob.Application.Resources;
using Microsoft.Extensions.Localization;
using Karajob.Application.Common;
using Karajob.Application.Persistence;
using Karajob.Domain.Common;
using Karajob.Domain.Goals;
using Karajob.Domain.Normalization;
using Karajob.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Application.Users;

public class UserService(IKarajobDbContextFactory dbContextFactory, TimeProvider timeProvider, IStringLocalizer<UserMessages> localizer)
{
  public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
    return users.OrderBy(u => u.CreatedAt).Select(ToDto).ToList();
  }

  public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    return user is null ? null : ToDto(user);
  }

  public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await db.Users.AnyAsync(u => u.Id == id, cancellationToken);
  }

  public async Task<UserDto> CreateAsync(UserInput input, CancellationToken cancellationToken = default)
  {
    Validate(input);
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var user = new User(input.Name, input.Color, timeProvider.GetUtcNow().UtcDateTime) { TimeZoneId = input.TimeZoneId };
    db.Users.Add(user);
    db.SearchGoals.Add(new SearchGoal(user.Id));
    await db.SaveChangesAsync(cancellationToken);
    return ToDto(user);
  }

  public async Task<UserDto> UpdateAsync(Guid id, UserInput input, CancellationToken cancellationToken = default)
  {
    Validate(input);
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
      ?? throw new KeyNotFoundException($"Profile {id} not found.");
    user.Name = input.Name;
    user.Color = input.Color;
    user.TimeZoneId = input.TimeZoneId;
    await db.SaveChangesAsync(cancellationToken);
    return ToDto(user);
  }

  public async Task<TimeZoneInfo> GetTimeZoneAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var timeZoneId = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.TimeZoneId).SingleOrDefaultAsync(cancellationToken)
      ?? throw new KeyNotFoundException($"Profile {userId} not found.");
    return UserTime.Find(timeZoneId);
  }

  private void Validate(UserInput input)
  {
    var errors = new Dictionary<string, string>();
    if (string.IsNullOrWhiteSpace(input.Name))
    {
      errors[nameof(UserInput.Name)] = localizer["Error.NameRequired"];
    }

    try
    {
      Normalize.HexColor(input.Color);
    }
    catch (ArgumentException)
    {
      errors[nameof(UserInput.Color)] = localizer["Error.ColorInvalid"];
    }

    try
    {
      UserTime.NormalizeId(input.TimeZoneId);
    }
    catch (ArgumentException)
    {
      errors[nameof(UserInput.TimeZoneId)] = localizer["Error.TimeZoneInvalid"];
    }

    if (errors.Count > 0)
    {
      throw new ValidationFailedException(errors);
    }
  }

  private static UserDto ToDto(User user)
  {
    return new UserDto(user.Id, user.Name, user.Color, user.TimeZoneId);
  }
}
