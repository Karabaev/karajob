using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
  public void Configure(EntityTypeBuilder<Currency> builder)
  {
    builder.Property(c => c.Code).HasMaxLength(3);
    builder.HasAlternateKey(c => c.Code);
    builder.HasIndex(c => c.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.RubCurrencyId, Code = "RUB", Name = "Российский рубль", NormalizedName = "российский рубль" },
      new { Id = SeedData.UsdCurrencyId, Code = "USD", Name = "Доллар США", NormalizedName = "доллар сша" },
      new { Id = SeedData.EurCurrencyId, Code = "EUR", Name = "Евро", NormalizedName = "евро" });
  }
}
