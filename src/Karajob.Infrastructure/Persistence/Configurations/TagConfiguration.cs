using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
  public void Configure(EntityTypeBuilder<Tag> builder)
  {
    builder.Property(t => t.Name).HasMaxLength(Tag.MaxNameLength);
    builder.Property(t => t.Color).HasMaxLength(7);
    builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
  }
}
