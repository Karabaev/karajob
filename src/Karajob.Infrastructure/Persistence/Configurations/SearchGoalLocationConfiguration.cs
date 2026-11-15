using Karajob.Domain.Catalogs;
using Karajob.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class SearchGoalLocationConfiguration : IEntityTypeConfiguration<SearchGoalLocation>
{
  public void Configure(EntityTypeBuilder<SearchGoalLocation> builder)
  {
    builder.HasIndex(l => new { l.SearchGoalId, l.LocationId }).IsUnique();
    builder.HasOne<Location>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
  }
}
