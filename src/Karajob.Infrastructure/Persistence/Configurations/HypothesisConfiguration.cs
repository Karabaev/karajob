using Karajob.Domain.Catalogs;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisConfiguration : IEntityTypeConfiguration<Hypothesis>
{
  public void Configure(EntityTypeBuilder<Hypothesis> builder)
  {
    builder.HasOne<CvDocument>().WithMany().HasForeignKey(h => h.PrimaryCvDocumentId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<Role>().WithMany().HasForeignKey(h => h.RoleId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<Language>().WithMany().HasForeignKey(h => h.LanguageId).OnDelete(DeleteBehavior.Restrict);

    builder.HasMany(h => h.Locations).WithOne().HasForeignKey(l => l.HypothesisId).OnDelete(DeleteBehavior.Cascade);
    // SetLocations diffs against the loaded collection.
    builder.Navigation(h => h.Locations).AutoInclude();

    builder.HasMany(h => h.Platforms).WithOne().HasForeignKey(p => p.HypothesisId).OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(h => h.Platforms).AutoInclude();

    builder.HasMany(h => h.Engines).WithOne().HasForeignKey(e => e.HypothesisId).OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(h => h.Engines).AutoInclude();
  }
}
