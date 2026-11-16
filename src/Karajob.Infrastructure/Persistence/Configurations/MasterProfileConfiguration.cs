using Karajob.Domain.MasterProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class MasterProfileConfiguration : IEntityTypeConfiguration<MasterProfile>
{
  public void Configure(EntityTypeBuilder<MasterProfile> builder)
  {
    builder.HasIndex(p => p.UserId).IsUnique();
    builder.HasMany(p => p.Versions).WithOne().HasForeignKey(v => v.MasterProfileId).OnDelete(DeleteBehavior.Cascade);
    // CurrentVersion and the restore check read the loaded collection.
    builder.Navigation(p => p.Versions).AutoInclude();
  }
}
