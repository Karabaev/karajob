using Karajob.Domain.Common;
using Karajob.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.Property(u => u.Color).HasMaxLength(7).HasDefaultValue(SeedData.OwnerColor);
    builder.Property(u => u.TimeZoneId).HasMaxLength(64).HasDefaultValue(UserTime.DefaultTimeZoneId);

    builder.HasData(new
    {
      Id = SeedData.OwnerId,
      Name = SeedData.OwnerName,
      Color = SeedData.OwnerColor,
      TimeZoneId = UserTime.DefaultTimeZoneId,
      CreatedAt = SeedData.OwnerCreatedAt,
    });
  }
}
