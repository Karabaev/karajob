using Karajob.Domain.Catalogs;
using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TrackEngineConfiguration : IEntityTypeConfiguration<TrackEngine>
{
  public void Configure(EntityTypeBuilder<TrackEngine> builder)
  {
    builder.HasIndex(e => new { e.TrackId, e.EngineId }).IsUnique();
    builder.HasOne<Engine>().WithMany().HasForeignKey(e => e.EngineId).OnDelete(DeleteBehavior.Restrict);
  }
}
