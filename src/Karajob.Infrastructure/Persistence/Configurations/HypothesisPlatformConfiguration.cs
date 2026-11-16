using Karajob.Domain.Catalogs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisPlatformConfiguration : IEntityTypeConfiguration<HypothesisPlatform>
{
  public void Configure(EntityTypeBuilder<HypothesisPlatform> builder)
  {
    builder.HasIndex(p => new { p.HypothesisId, p.PlatformId }).IsUnique();
    builder.HasOne<Platform>().WithMany().HasForeignKey(p => p.PlatformId).OnDelete(DeleteBehavior.Restrict);
  }
}
