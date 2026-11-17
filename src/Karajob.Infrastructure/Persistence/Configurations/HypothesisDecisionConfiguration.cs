using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisDecisionConfiguration : IEntityTypeConfiguration<HypothesisDecision>
{
  public void Configure(EntityTypeBuilder<HypothesisDecision> builder)
  {
    builder.HasOne<Hypothesis>().WithMany().HasForeignKey(d => d.HypothesisId).OnDelete(DeleteBehavior.Restrict);
    builder.HasIndex(d => new { d.HypothesisId, d.DecidedAt });
  }
}
