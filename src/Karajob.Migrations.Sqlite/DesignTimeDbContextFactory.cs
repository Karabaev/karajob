using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Karajob.Migrations.Sqlite;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KarajobDbContext>
{
  public KarajobDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<KarajobDbContext>()
      .UseSqlite("Data Source=design-time.db", b => b.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name))
      .Options;
    return new KarajobDbContext(options, new FixedCurrentUser(SeedData.OwnerId));
  }
}
