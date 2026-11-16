using Karajob.Domain.Catalogs;
using Karajob.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class SearchGoalPlatformConfiguration : IEntityTypeConfiguration<SearchGoalPlatform>
{
  public void Configure(EntityTypeBuilder<SearchGoalPlatform> builder)
  {
    builder.HasIndex(p => new { p.SearchGoalId, p.PlatformId }).IsUnique();
    builder.HasOne<Platform>().WithMany().HasForeignKey(p => p.PlatformId).OnDelete(DeleteBehavior.Restrict);
  }
}
