using Karajob.Domain.Applications;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
  public void Configure(EntityTypeBuilder<JobApplication> builder)
  {
    builder.HasOne<Hypothesis>().WithMany().HasForeignKey(a => a.HypothesisId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<CvVersion>().WithMany().HasForeignKey(a => a.CvVersionId).OnDelete(DeleteBehavior.Restrict);
  }
}
