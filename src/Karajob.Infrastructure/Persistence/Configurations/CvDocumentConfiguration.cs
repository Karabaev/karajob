using Karajob.Domain.Catalogs;
using Karajob.Domain.Cvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CvDocumentConfiguration : IEntityTypeConfiguration<CvDocument>
{
  public void Configure(EntityTypeBuilder<CvDocument> builder)
  {
    builder.HasOne<Language>().WithMany().HasForeignKey(d => d.LanguageId).OnDelete(DeleteBehavior.Restrict);
    builder.HasMany(d => d.Versions).WithOne().HasForeignKey(v => v.CvDocumentId).OnDelete(DeleteBehavior.Cascade);
  }
}
