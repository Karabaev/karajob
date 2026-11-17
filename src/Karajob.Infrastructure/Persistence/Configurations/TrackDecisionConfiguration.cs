using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TrackDecisionConfiguration : IEntityTypeConfiguration<TrackDecision>
{
  public void Configure(EntityTypeBuilder<TrackDecision> builder)
  {
    builder.HasOne<Track>().WithMany().HasForeignKey(d => d.TrackId).OnDelete(DeleteBehavior.Restrict);
    builder.HasIndex(d => new { d.TrackId, d.DecidedAt });
  }
}
