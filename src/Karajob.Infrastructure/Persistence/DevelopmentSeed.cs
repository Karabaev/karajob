namespace Karajob.Infrastructure.Persistence;

public static class DevelopmentSeed
{
  public static Task SeedAsync(KarajobDbContext context, DateTime utcNow, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
