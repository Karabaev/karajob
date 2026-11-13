using Karajob.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class UserCompanyNoteConfiguration : IEntityTypeConfiguration<UserCompanyNote>
{
  public void Configure(EntityTypeBuilder<UserCompanyNote> builder)
  {
    builder.HasOne<Company>().WithMany().HasForeignKey(n => n.CompanyId).OnDelete(DeleteBehavior.Restrict);
    builder.HasIndex(n => new { n.UserId, n.CompanyId }).IsUnique();
  }
}
