using Karajob.Domain.Applications;
using Karajob.Domain.Catalogs;
using Karajob.Domain.Companies;
using Karajob.Domain.Vacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class VacancyConfiguration : IEntityTypeConfiguration<Vacancy>
{
  public void Configure(EntityTypeBuilder<Vacancy> builder)
  {
    builder.HasIndex(v => new { v.UserId, v.Url }).IsUnique();
    builder.HasOne<Company>().WithMany().HasForeignKey(v => v.CompanyId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<Role>().WithMany().HasForeignKey(v => v.RoleId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<Location>().WithMany().HasForeignKey(v => v.LocationId).OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<Language>().WithMany().HasForeignKey(v => v.LanguageId).OnDelete(DeleteBehavior.Restrict);
    builder.ComplexProperty(v => v.Salary, salary =>
    {
      // Get-only properties are not mapped by convention; mapping them lets EF bind the constructor.
      salary.Property(s => s.Min);
      salary.Property(s => s.Max);
      salary.Property(s => s.Currency);
    });

    builder.HasMany(v => v.Matches).WithOne().HasForeignKey(m => m.VacancyId).OnDelete(DeleteBehavior.Cascade);
    builder.HasMany(v => v.Events).WithOne().HasForeignKey(e => e.VacancyId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne(v => v.Application)
      .WithOne()
      .HasForeignKey<JobApplication>(a => a.VacancyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Stage transitions read these: MoveTo/Apply check matches, Close and reopening from Closed set the outcome.
    builder.Navigation(v => v.Matches).AutoInclude();
    builder.Navigation(v => v.Application).AutoInclude();
  }
}
