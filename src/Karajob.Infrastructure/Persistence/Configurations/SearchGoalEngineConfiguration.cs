using Karajob.Domain.Catalogs;
using Karajob.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class SearchGoalEngineConfiguration : IEntityTypeConfiguration<SearchGoalEngine>
{
  public void Configure(EntityTypeBuilder<SearchGoalEngine> builder)
  {
    builder.HasIndex(e => new { e.SearchGoalId, e.EngineId }).IsUnique();
    builder.HasOne<Engine>().WithMany().HasForeignKey(e => e.EngineId).OnDelete(DeleteBehavior.Restrict);
  }
}
