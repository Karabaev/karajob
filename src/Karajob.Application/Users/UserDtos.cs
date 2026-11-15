namespace Karajob.Application.Users;

public record UserDto(Guid Id, string Name, string Color);

public record UserInput(string Name, string Color);
