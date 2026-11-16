using Karajob.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karajob.Infrastructure.Persistence.Configurations;

public class EngineConfiguration : IEntityTypeConfiguration<Engine>
{
  public void Configure(EntityTypeBuilder<Engine> builder)
  {
    builder.HasIndex(e => e.NormalizedName).IsUnique();
    builder.HasData(
      new { Id = SeedData.UnityEngineId, Name = "Unity", NormalizedName = "unity" },
      new { Id = SeedData.UnrealEngineId, Name = "Unreal Engine", NormalizedName = "unreal engine" },
      new { Id = SeedData.GodotEngineId, Name = "Godot", NormalizedName = "godot" },
      new { Id = SeedData.DefoldEngineId, Name = "Defold", NormalizedName = "defold" },
      new { Id = SeedData.CocosEngineId, Name = "Cocos", NormalizedName = "cocos" },
      new { Id = SeedData.ProprietaryEngineId, Name = "Собственный", NormalizedName = "собственный" });
  }
}
