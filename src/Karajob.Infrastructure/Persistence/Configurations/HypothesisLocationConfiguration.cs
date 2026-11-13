using Karajob.Domain.Catalogs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisLocationConfiguration : IEntityTypeConfiguration<HypothesisLocation>
{
  public void Configure(EntityTypeBuilder<HypothesisLocation> builder)
  {
    builder.HasIndex(l => new { l.HypothesisId, l.LocationId }).IsUnique();
    builder.HasOne<Location>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
  }
}
