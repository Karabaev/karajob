using Karajob.Domain.Catalogs;
using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TrackLocationConfiguration : IEntityTypeConfiguration<TrackLocation>
{
  public void Configure(EntityTypeBuilder<TrackLocation> builder)
  {
    builder.HasIndex(l => new { l.TrackId, l.LocationId }).IsUnique();
    builder.HasOne<Location>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
  }
}
