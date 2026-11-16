using Karajob.Domain.Catalogs;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class VacancyEngineConfiguration : IEntityTypeConfiguration<VacancyEngine>
{
  public void Configure(EntityTypeBuilder<VacancyEngine> builder)
  {
    builder.HasIndex(e => new { e.VacancyId, e.EngineId }).IsUnique();
    builder.HasOne<Engine>().WithMany().HasForeignKey(e => e.EngineId).OnDelete(DeleteBehavior.Restrict);
  }
}
