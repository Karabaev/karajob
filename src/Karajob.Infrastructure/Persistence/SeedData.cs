namespace Karajob.Infrastructure.Persistence;

public static class SeedData
{
  public static readonly Guid OwnerId = new("019a7c1e-0000-7000-8000-000000000001");
  public const string OwnerName = "Owner";
  public static readonly DateTime OwnerCreatedAt = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

  public static readonly Guid UnityDeveloperRoleId = new("019a7c1e-0000-7000-8000-000000000101");
  public static readonly Guid UnrealDeveloperRoleId = new("019a7c1e-0000-7000-8000-000000000102");

  public static readonly Guid EuLocationId = new("019a7c1e-0000-7000-8000-000000000201");
  public static readonly Guid UsaLocationId = new("019a7c1e-0000-7000-8000-000000000202");
  public static readonly Guid SerbiaLocationId = new("019a7c1e-0000-7000-8000-000000000203");

  public static readonly Guid RussianLanguageId = new("019a7c1e-0000-7000-8000-000000000301");
  public static readonly Guid EnglishLanguageId = new("019a7c1e-0000-7000-8000-000000000302");
}
