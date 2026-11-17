using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
  public void Configure(EntityTypeBuilder<Location> builder)
  {
    builder.Property(l => l.TimeZoneId).HasMaxLength(64);
    builder.HasIndex(l => l.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.EuLocationId, Name = "EU", NormalizedName = "eu", TimeZoneId = (string?)null },
      new { Id = SeedData.UsaLocationId, Name = "USA", NormalizedName = "usa", TimeZoneId = (string?)null },
      new { Id = SeedData.SerbiaLocationId, Name = "Serbia", NormalizedName = "serbia", TimeZoneId = (string?)"Europe/Belgrade" });
  }
}
