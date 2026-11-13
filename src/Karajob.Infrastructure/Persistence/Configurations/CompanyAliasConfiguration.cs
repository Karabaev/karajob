using Karajob.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CompanyAliasConfiguration : IEntityTypeConfiguration<CompanyAlias>
{
  public void Configure(EntityTypeBuilder<CompanyAlias> builder)
  {
    builder.HasIndex(a => a.Value).IsUnique();
  }
}
