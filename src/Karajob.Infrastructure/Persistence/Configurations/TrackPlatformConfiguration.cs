using Karajob.Domain.Catalogs;
using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TrackPlatformConfiguration : IEntityTypeConfiguration<TrackPlatform>
{
  public void Configure(EntityTypeBuilder<TrackPlatform> builder)
  {
    builder.HasIndex(p => new { p.TrackId, p.PlatformId }).IsUnique();
    builder.HasOne<Platform>().WithMany().HasForeignKey(p => p.PlatformId).OnDelete(DeleteBehavior.Restrict);
  }
}
