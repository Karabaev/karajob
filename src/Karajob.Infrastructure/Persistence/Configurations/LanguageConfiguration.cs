using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
  public void Configure(EntityTypeBuilder<Language> builder)
  {
    builder.HasIndex(l => l.NormalizedName).IsUnique();
    builder.HasIndex(l => l.Code).IsUnique();
    builder.HasData(
      new { Id = SeedData.RussianLanguageId, Code = "ru", Name = "Русский", NormalizedName = "русский" },
      new { Id = SeedData.EnglishLanguageId, Code = "en", Name = "English", NormalizedName = "english" });
  }
}
