namespace Karajob.Infrastructure.Persistence;

public static class SeedData
{
  public static readonly Guid OwnerId = new("019a7c1e-0000-7000-8000-000000000001");
  public const string OwnerName = "Owner";
  public static readonly DateTime OwnerCreatedAt = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
}
