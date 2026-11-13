using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class HypothesisConfiguration : IEntityTypeConfiguration<Hypothesis>
{
  public void Configure(EntityTypeBuilder<Hypothesis> builder)
  {
    builder.HasOne<CvDocument>().WithMany().HasForeignKey(h => h.PrimaryCvDocumentId).OnDelete(DeleteBehavior.Restrict);
  }
}
