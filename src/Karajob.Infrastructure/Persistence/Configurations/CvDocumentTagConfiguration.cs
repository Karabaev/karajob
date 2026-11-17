using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CvDocumentTagConfiguration : IEntityTypeConfiguration<CvDocumentTag>
{
  public void Configure(EntityTypeBuilder<CvDocumentTag> builder)
  {
    builder.HasIndex(t => new { t.CvDocumentId, t.TagId }).IsUnique();
    builder.HasOne<Tag>().WithMany().HasForeignKey(t => t.TagId).OnDelete(DeleteBehavior.Cascade);
  }
}
