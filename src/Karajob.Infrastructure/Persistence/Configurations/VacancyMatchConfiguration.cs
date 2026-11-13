using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class VacancyMatchConfiguration : IEntityTypeConfiguration<VacancyMatch>
{
  public void Configure(EntityTypeBuilder<VacancyMatch> builder)
  {
    builder.HasIndex(m => new { m.VacancyId, m.HypothesisId }).IsUnique();
    builder.HasOne<Hypothesis>().WithMany().HasForeignKey(m => m.HypothesisId).OnDelete(DeleteBehavior.Restrict);
  }
}
