using Karajob.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
  public void Configure(EntityTypeBuilder<Company> builder)
  {
    builder.HasIndex(c => c.Slug).IsUnique();

    builder.HasMany(c => c.Domains).WithOne().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Cascade);
    builder.HasMany(c => c.Aliases).WithOne().HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Cascade);

    // AddDomain/AddAlias deduplicate against the loaded collections.
    builder.Navigation(c => c.Domains).AutoInclude();
    builder.Navigation(c => c.Aliases).AutoInclude();
  }
}
