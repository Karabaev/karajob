using Karajob.Domain.Applications;
using Karajob.Domain.Cvs;
using Karajob.Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
  public void Configure(EntityTypeBuilder<JobApplication> builder)
  {
    builder.HasOne<Track>().WithMany().HasForeignKey(a => a.TrackId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<CvVersion>().WithMany().HasForeignKey(a => a.CvVersionId).OnDelete(DeleteBehavior.Restrict);
  }
}
