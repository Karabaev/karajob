using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
  public void Configure(EntityTypeBuilder<Location> builder)
  {
    builder.HasIndex(l => l.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.EuLocationId, Name = "EU", NormalizedName = "eu" },
      new { Id = SeedData.UsaLocationId, Name = "USA", NormalizedName = "usa" },
      new { Id = SeedData.SerbiaLocationId, Name = "Serbia", NormalizedName = "serbia" });
  }
}
