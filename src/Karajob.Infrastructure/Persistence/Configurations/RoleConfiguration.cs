using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
  public void Configure(EntityTypeBuilder<Role> builder)
  {
    builder.HasIndex(r => r.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.UnityDeveloperRoleId, Name = "Unity Developer", NormalizedName = "unity developer" },
      new { Id = SeedData.UnrealDeveloperRoleId, Name = "Unreal Developer", NormalizedName = "unreal developer" });
  }
}
