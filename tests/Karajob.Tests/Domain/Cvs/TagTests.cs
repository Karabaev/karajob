using Karajob.Domain.Cvs;

namespace Karajob.Tests.Domain.Cvs;

public class TagTests
{
  private static Tag Create(string name = "unity") => new(Guid.NewGuid(), name);

  [Fact]
  public void Ctor_Valid_SetsFields()
  {
    var userId = Guid.NewGuid();

    var tag = new Tag(userId, "unity");

    Assert.NotEqual(Guid.Empty, tag.Id);
    Assert.Equal(userId, tag.UserId);
    Assert.Equal("unity", tag.Name);
    Assert.Null(tag.Color);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Tag(Guid.Empty, "unity"));
  }

  [Fact]
  public void Ctor_MixedCaseWithSpaces_TrimsAndLowercases()
  {
    Assert.Equal("c#", Create("  C#  ").Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => Create(name));
  }

  [Fact]
  public void Ctor_NameOfFiftyCharacters_IsAccepted()
  {
    var name = new string('a', 50);

    Assert.Equal(name, Create(name).Name);
  }

  [Fact]
  public void Ctor_NameOfFiftyOneCharacters_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => Create(new string('a', 51)));
  }

  [Fact]
  public void Ctor_NameLongOnlyBeforeTrim_IsAccepted()
  {
    var name = " " + new string('a', 50) + " ";

    Assert.Equal(new string('a', 50), Create(name).Name);
  }

  [Fact]
  public void NormalizeName_MixedCase_TrimsAndLowercases()
  {
    Assert.Equal("game dev", Tag.NormalizeName("  Game DEV "));
  }

  [Fact]
  public void NormalizeName_Blank_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => Tag.NormalizeName("  "));
  }

  [Fact]
  public void NormalizeName_TooLong_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => Tag.NormalizeName(new string('a', 51)));
  }

  [Fact]
  public void Rename_Valid_NormalizesName()
  {
    var tag = Create();

    tag.Rename("  Unreal ");

    Assert.Equal("unreal", tag.Name);
  }

  [Fact]
  public void Rename_Blank_ThrowsAndKeepsName()
  {
    var tag = Create();

    Assert.ThrowsAny<ArgumentException>(() => tag.Rename(" "));
    Assert.Equal("unity", tag.Name);
  }

  [Fact]
  public void Rename_TooLong_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => Create().Rename(new string('a', 51)));
  }

  [Fact]
  public void SetColor_LowercaseHex_StoresColor()
  {
    var tag = Create();

    tag.SetColor("#12ab9f");

    Assert.Equal("#12ab9f", tag.Color);
  }

  [Fact]
  public void SetColor_UppercaseHex_StoresLowercase()
  {
    var tag = Create();

    tag.SetColor("#12AB9F");

    Assert.Equal("#12ab9f", tag.Color);
  }

  [Fact]
  public void SetColor_Null_ClearsColor()
  {
    var tag = Create();
    tag.SetColor("#12ab9f");

    tag.SetColor(null);

    Assert.Null(tag.Color);
  }

  [Theory]
  [InlineData("")]
  [InlineData("red")]
  [InlineData("12ab9f")]
  [InlineData("#12ab9")]
  [InlineData("#12ab9f0")]
  [InlineData("#12ab9g")]
  [InlineData("#fff")]
  public void SetColor_Invalid_Throws(string color)
  {
    Assert.ThrowsAny<ArgumentException>(() => Create().SetColor(color));
  }

  [Fact]
  public void SetColor_InvalidAfterValid_KeepsPreviousColor()
  {
    var tag = Create();
    tag.SetColor("#12ab9f");

    Assert.ThrowsAny<ArgumentException>(() => tag.SetColor("nope"));
    Assert.Equal("#12ab9f", tag.Color);
  }
}
