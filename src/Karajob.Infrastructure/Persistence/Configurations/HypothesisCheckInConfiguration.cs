using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisCheckInConfiguration : IEntityTypeConfiguration<HypothesisCheckIn>
{
  public void Configure(EntityTypeBuilder<HypothesisCheckIn> builder)
  {
    builder.HasOne<Hypothesis>().WithMany().HasForeignKey(c => c.HypothesisId).OnDelete(DeleteBehavior.Restrict);
    builder.HasIndex(c => new { c.HypothesisId, c.Week }).IsUnique();
  }
}
