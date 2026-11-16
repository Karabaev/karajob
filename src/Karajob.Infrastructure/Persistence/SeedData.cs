namespace Karajob.Infrastructure.Persistence;

public static class SeedData
{
  public static readonly Guid OwnerId = new("019a7c1e-0000-7000-8000-000000000001");
  public const string OwnerName = "Owner";
  public const string OwnerColor = "#7e57c2";
  public static readonly DateTime OwnerCreatedAt = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
  public static readonly Guid OwnerSearchGoalId = new("019a7c1e-0000-7000-8000-000000000401");

  public static readonly Guid UnityDeveloperRoleId = new("019a7c1e-0000-7000-8000-000000000101");
  public static readonly Guid UnrealDeveloperRoleId = new("019a7c1e-0000-7000-8000-000000000102");

  public static readonly Guid EuLocationId = new("019a7c1e-0000-7000-8000-000000000201");
  public static readonly Guid UsaLocationId = new("019a7c1e-0000-7000-8000-000000000202");
  public static readonly Guid SerbiaLocationId = new("019a7c1e-0000-7000-8000-000000000203");

  public static readonly Guid RussianLanguageId = new("019a7c1e-0000-7000-8000-000000000301");
  public static readonly Guid EnglishLanguageId = new("019a7c1e-0000-7000-8000-000000000302");

  public static readonly Guid PcPlatformId = new("019a7c1e-0000-7000-8000-000000000501");
  public static readonly Guid ConsolesPlatformId = new("019a7c1e-0000-7000-8000-000000000502");
  public static readonly Guid MobilePlatformId = new("019a7c1e-0000-7000-8000-000000000503");
  public static readonly Guid VrPlatformId = new("019a7c1e-0000-7000-8000-000000000504");
  public static readonly Guid WebPlatformId = new("019a7c1e-0000-7000-8000-000000000505");

  public static readonly Guid RubCurrencyId = new("019a7c1e-0000-7000-8000-000000000601");
  public static readonly Guid UsdCurrencyId = new("019a7c1e-0000-7000-8000-000000000602");
  public static readonly Guid EurCurrencyId = new("019a7c1e-0000-7000-8000-000000000603");

  public static readonly Guid UnityEngineId = new("019a7c1e-0000-7000-8000-000000000701");
  public static readonly Guid UnrealEngineId = new("019a7c1e-0000-7000-8000-000000000702");
  public static readonly Guid GodotEngineId = new("019a7c1e-0000-7000-8000-000000000703");
  public static readonly Guid DefoldEngineId = new("019a7c1e-0000-7000-8000-000000000704");
  public static readonly Guid CocosEngineId = new("019a7c1e-0000-7000-8000-000000000705");
  public static readonly Guid ProprietaryEngineId = new("019a7c1e-0000-7000-8000-000000000706");
}
