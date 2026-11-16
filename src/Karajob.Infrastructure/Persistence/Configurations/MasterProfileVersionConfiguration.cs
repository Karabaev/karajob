using Karajob.Domain.MasterProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class MasterProfileVersionConfiguration : IEntityTypeConfiguration<MasterProfileVersion>
{
  public void Configure(EntityTypeBuilder<MasterProfileVersion> builder)
  {
    builder.HasOne<MasterProfileVersion>().WithMany().HasForeignKey(v => v.RestoredFromVersionId).OnDelete(DeleteBehavior.Restrict);
  }
}
