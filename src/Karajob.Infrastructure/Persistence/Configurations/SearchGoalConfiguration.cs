using Karajob.Domain.Catalogs;
using Karajob.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class SearchGoalConfiguration : IEntityTypeConfiguration<SearchGoal>
{
  public void Configure(EntityTypeBuilder<SearchGoal> builder)
  {
    builder.HasIndex(g => g.UserId).IsUnique();
    builder.HasOne<Role>().WithMany().HasForeignKey(g => g.RoleId).OnDelete(DeleteBehavior.Restrict);
    builder.Property(g => g.IncomeCurrency).HasMaxLength(3);

    builder.HasMany(g => g.Locations).WithOne().HasForeignKey(l => l.SearchGoalId).OnDelete(DeleteBehavior.Cascade);
    // SetLocations diffs against the loaded collection.
    builder.Navigation(g => g.Locations).AutoInclude();

    builder.HasData(new
    {
      Id = SeedData.OwnerSearchGoalId,
      UserId = SeedData.OwnerId,
      SearchGoalDefaults.WeeklyApplicationTarget,
      SearchGoalDefaults.MarketThresholdPerWeek,
      SearchGoalDefaults.ConversionThresholdPermille,
      SearchGoalDefaults.MinSampleSize,
      SearchGoalDefaults.MaturationDays,
    });
  }
}
