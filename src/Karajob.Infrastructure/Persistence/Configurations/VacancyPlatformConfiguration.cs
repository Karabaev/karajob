using Karajob.Domain.Catalogs;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class VacancyPlatformConfiguration : IEntityTypeConfiguration<VacancyPlatform>
{
  public void Configure(EntityTypeBuilder<VacancyPlatform> builder)
  {
    builder.HasIndex(p => new { p.VacancyId, p.PlatformId }).IsUnique();
    builder.HasOne<Platform>().WithMany().HasForeignKey(p => p.PlatformId).OnDelete(DeleteBehavior.Restrict);
  }
}
