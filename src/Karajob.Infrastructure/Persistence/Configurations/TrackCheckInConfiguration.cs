using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TrackCheckInConfiguration : IEntityTypeConfiguration<TrackCheckIn>
{
  public void Configure(EntityTypeBuilder<TrackCheckIn> builder)
  {
    builder.HasOne<Track>().WithMany().HasForeignKey(c => c.TrackId).OnDelete(DeleteBehavior.Restrict);
    builder.HasIndex(c => new { c.TrackId, c.Week }).IsUnique();
  }
}
