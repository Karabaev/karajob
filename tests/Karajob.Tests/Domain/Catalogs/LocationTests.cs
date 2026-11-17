using Karajob.Domain.Catalogs;

namespace Karajob.Tests.Domain.Catalogs;

public class LocationTests
{
  [Fact]
  public void Ctor_NewLocation_HasNoTimeZone()
  {
    var location = new Location("Serbia");

    Assert.Null(location.TimeZoneId);
  }

  [Theory]
  [InlineData("Europe/Belgrade", "Europe/Belgrade")]
  [InlineData("  Europe/Belgrade ", "Europe/Belgrade")]
  [InlineData("UTC", "UTC")]
  [InlineData("W. Europe Standard Time", "Europe/Berlin")]
  public void SetTimeZone_ValidId_StoresNormalizedIanaId(string id, string expected)
  {
    var location = new Location("Serbia");

    location.SetTimeZone(id);

    Assert.Equal(expected, location.TimeZoneId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void SetTimeZone_NullOrBlank_ClearsExistingZone(string? id)
  {
    var location = new Location("Serbia");
    location.SetTimeZone("Europe/Belgrade");

    location.SetTimeZone(id);

    Assert.Null(location.TimeZoneId);
  }

  [Fact]
  public void SetTimeZone_UnknownId_ThrowsArgumentException()
  {
    var location = new Location("Serbia");

    Assert.ThrowsAny<ArgumentException>(() => location.SetTimeZone("Mars/Olympus_Mons"));
  }

  [Fact]
  public void SetTimeZone_UnknownId_KeepsPreviousZone()
  {
    var location = new Location("Serbia");
    location.SetTimeZone("Europe/Belgrade");

    Assert.ThrowsAny<ArgumentException>(() => location.SetTimeZone("Mars/Olympus_Mons"));

    Assert.Equal("Europe/Belgrade", location.TimeZoneId);
  }
}
