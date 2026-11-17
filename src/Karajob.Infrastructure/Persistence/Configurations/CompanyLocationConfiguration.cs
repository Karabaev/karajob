using Karajob.Domain.Catalogs;
using Karajob.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CompanyLocationConfiguration : IEntityTypeConfiguration<CompanyLocation>
{
  public void Configure(EntityTypeBuilder<CompanyLocation> builder)
  {
    builder.HasIndex(l => new { l.CompanyId, l.LocationId }).IsUnique();
    builder.HasOne<Location>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
  }
}
