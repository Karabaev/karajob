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
    builder.HasOne<Currency>().WithMany().HasForeignKey(g => g.IncomeCurrency).HasPrincipalKey(c => c.Code).OnDelete(DeleteBehavior.Restrict);

    // Existing profiles get the default threshold when the column is added.
    builder.Property(g => g.WellBeingThreshold).HasDefaultValue(2);

    builder.HasMany(g => g.Locations).WithOne().HasForeignKey(l => l.SearchGoalId).OnDelete(DeleteBehavior.Cascade);
    // SetLocations diffs against the loaded collection.
    builder.Navigation(g => g.Locations).AutoInclude();

    builder.HasMany(g => g.Platforms).WithOne().HasForeignKey(p => p.SearchGoalId).OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(g => g.Platforms).AutoInclude();

    builder.HasMany(g => g.Engines).WithOne().HasForeignKey(e => e.SearchGoalId).OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(g => g.Engines).AutoInclude();

    // Literals, not SearchGoalDefaults: changing a default for new profiles must not emit UpdateData over the Owner's edited goal.
    builder.HasData(new
    {
      Id = SeedData.OwnerSearchGoalId,
      UserId = SeedData.OwnerId,
      WeeklyApplicationTarget = 10,
      MarketThresholdPerWeek = 10,
      ConversionThresholdPermille = 30,
      MinSampleSize = 30,
      MaturationDays = 14,
      WellBeingThreshold = 2,
    });
  }
}
