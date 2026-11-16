using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
  public void Configure(EntityTypeBuilder<Platform> builder)
  {
    builder.HasIndex(p => p.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.PcPlatformId, Name = "PC", NormalizedName = "pc" },
      new { Id = SeedData.ConsolesPlatformId, Name = "Consoles", NormalizedName = "consoles" },
      new { Id = SeedData.MobilePlatformId, Name = "Mobile", NormalizedName = "mobile" },
      new { Id = SeedData.VrPlatformId, Name = "VR", NormalizedName = "vr" },
      new { Id = SeedData.WebPlatformId, Name = "Web", NormalizedName = "web" });
  }
}
