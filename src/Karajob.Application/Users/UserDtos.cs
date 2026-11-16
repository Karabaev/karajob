using Karajob.Domain.Common;

namespace Karajob.Application.Users;

public record UserDto(Guid Id, string Name, string Color, string TimeZoneId = UserTime.DefaultTimeZoneId);

public record UserInput(string Name, string Color, string TimeZoneId = UserTime.DefaultTimeZoneId);
