using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class VacancyMatchConfiguration : IEntityTypeConfiguration<VacancyMatch>
{
  public void Configure(EntityTypeBuilder<VacancyMatch> builder)
  {
    builder.HasIndex(m => new { m.VacancyId, m.TrackId }).IsUnique();
    builder.HasOne<Track>().WithMany().HasForeignKey(m => m.TrackId).OnDelete(DeleteBehavior.Restrict);
  }
}
