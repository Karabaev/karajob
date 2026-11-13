using Karajob.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CompanyDomainConfiguration : IEntityTypeConfiguration<CompanyDomain>
{
  public void Configure(EntityTypeBuilder<CompanyDomain> builder)
  {
    builder.HasIndex(d => d.Value).IsUnique();
  }
}
