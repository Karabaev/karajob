using Karajob.Domain.Catalogs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisEngineConfiguration : IEntityTypeConfiguration<HypothesisEngine>
{
  public void Configure(EntityTypeBuilder<HypothesisEngine> builder)
  {
    builder.HasIndex(e => new { e.HypothesisId, e.EngineId }).IsUnique();
    builder.HasOne<Engine>().WithMany().HasForeignKey(e => e.EngineId).OnDelete(DeleteBehavior.Restrict);
  }
}
