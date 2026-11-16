using Karajob.Domain.Catalogs;

namespace Karajob.Tests.Domain.Catalogs;

public class CatalogEntryTests
{
  public static TheoryData<Func<string, CatalogEntry>> Factories => new()
  {
    name => new Role(name),
    name => new Location(name),
    name => new Language("ru", name),
    name => new Currency("USD", name),
    name => new Engine(name),
  };

  [Fact]
  public void Ctor_Role_SetsIdNameAndNormalizedName()
  {
    var role = new Role("Unity Developer");

    Assert.NotEqual(Guid.Empty, role.Id);
    Assert.Equal("Unity Developer", role.Name);
    Assert.Equal("unity developer", role.NormalizedName);
  }

  [Fact]
  public void Ctor_NameWithExtraWhitespace_TrimsNameAndCollapsesNormalizedName()
  {
    var location = new Location("  Unity   Developer ");

    Assert.Equal("Unity   Developer", location.Name);
    Assert.Equal("unity developer", location.NormalizedName);
  }

  [Fact]
  public void Ctor_CyrillicName_LowerCasesNormalizedName()
  {
    var language = new Language("ru", "Русский");

    Assert.Equal("Русский", language.Name);
    Assert.Equal("русский", language.NormalizedName);
  }

  [Theory]
  [MemberData(nameof(Factories))]
  public void Ctor_BlankName_Throws(Func<string, CatalogEntry> create)
  {
    Assert.ThrowsAny<ArgumentException>(() => create(""));
    Assert.ThrowsAny<ArgumentException>(() => create("   "));
  }

  [Fact]
  public void Rename_ValidName_UpdatesNameAndNormalizedName()
  {
    var role = new Role("Unity Developer");

    role.Rename("  Unreal   Engine Dev ");

    Assert.Equal("Unreal   Engine Dev", role.Name);
    Assert.Equal("unreal engine dev", role.NormalizedName);
  }

  [Fact]
  public void Rename_KeepsId()
  {
    var role = new Role("Unity Developer");
    var id = role.Id;

    role.Rename("Other");

    Assert.Equal(id, role.Id);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Rename_BlankName_ThrowsAndKeepsPreviousValues(string name)
  {
    var role = new Role("Unity Developer");

    Assert.ThrowsAny<ArgumentException>(() => role.Rename(name));

    Assert.Equal("Unity Developer", role.Name);
    Assert.Equal("unity developer", role.NormalizedName);
  }

  [Fact]
  public void Ctor_Language_NormalizesCode()
  {
    var language = new Language(" RU ", "Русский");

    Assert.Equal("ru", language.Code);
  }

  [Theory]
  [InlineData("")]
  [InlineData("r")]
  [InlineData("russ")]
  [InlineData("r1")]
  [InlineData("ру")]
  public void Ctor_Language_InvalidCode_Throws(string code)
  {
    Assert.ThrowsAny<ArgumentException>(() => new Language(code, "Русский"));
  }

  [Fact]
  public void Rename_Language_KeepsCode()
  {
    var language = new Language("ru", "Русский");

    language.Rename("Russian");

    Assert.Equal("ru", language.Code);
    Assert.Equal("russian", language.NormalizedName);
  }

  [Fact]
  public void Ctor_Currency_NormalizesCodeToUpperCase()
  {
    var currency = new Currency(" usd ", "Доллар США");

    Assert.Equal("USD", currency.Code);
    Assert.Equal("доллар сша", currency.NormalizedName);
  }

  [Theory]
  [InlineData("")]
  [InlineData("US")]
  [InlineData("USDT")]
  [InlineData("U5D")]
  public void Ctor_Currency_InvalidCode_Throws(string code)
  {
    Assert.ThrowsAny<ArgumentException>(() => new Currency(code, "Доллар США"));
  }

  [Fact]
  public void Rename_Currency_KeepsCode()
  {
    var currency = new Currency("USD", "Доллар США");

    currency.Rename("US Dollar");

    Assert.Equal("USD", currency.Code);
    Assert.Equal("us dollar", currency.NormalizedName);
  }

  [Fact]
  public void Ctor_Engine_SetsIdNameAndNormalizedName()
  {
    var engine = new Engine("  Unreal   Engine ");

    Assert.NotEqual(Guid.Empty, engine.Id);
    Assert.Equal("Unreal   Engine", engine.Name);
    Assert.Equal("unreal engine", engine.NormalizedName);
  }
}
