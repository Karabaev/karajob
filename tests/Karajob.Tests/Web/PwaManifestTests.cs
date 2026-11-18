using System.Buffers.Binary;
using System.Text.Json;
using Karajob.Tests.Localization;

namespace Karajob.Tests.Web;

public class PwaManifestTests
{
  private static readonly string WwwRoot = Path.Combine(RepositoryPaths.Src, "Karajob.Web", "wwwroot");

  [Fact]
  public void Manifest_Exists_AndParsesAsJson()
  {
    using var manifest = LoadManifest();

    Assert.Equal(JsonValueKind.Object, manifest.RootElement.ValueKind);
  }

  [Fact]
  public void Manifest_HasNameAndShortName()
  {
    using var manifest = LoadManifest();

    Assert.Equal("Karajob", manifest.RootElement.GetProperty("name").GetString());
    Assert.Equal("Karajob", manifest.RootElement.GetProperty("short_name").GetString());
  }

  [Fact]
  public void Manifest_HasStartUrlScopeAndStandaloneDisplay()
  {
    using var manifest = LoadManifest();

    Assert.Equal("/", manifest.RootElement.GetProperty("start_url").GetString());
    Assert.Equal("/", manifest.RootElement.GetProperty("scope").GetString());
    Assert.Equal("standalone", manifest.RootElement.GetProperty("display").GetString());
  }

  [Fact]
  public void Manifest_ShareTarget_UsesGetToQuickAddWithParams()
  {
    using var manifest = LoadManifest();

    var target = manifest.RootElement.GetProperty("share_target");
    Assert.Equal("/quick-add", target.GetProperty("action").GetString());
    Assert.Equal("GET", target.GetProperty("method").GetString());
    var parameters = target.GetProperty("params");
    Assert.Equal("title", parameters.GetProperty("title").GetString());
    Assert.Equal("text", parameters.GetProperty("text").GetString());
    Assert.Equal("url", parameters.GetProperty("url").GetString());
  }

  [Theory]
  [InlineData("192x192", "any")]
  [InlineData("512x512", "any")]
  [InlineData("512x512", "maskable")]
  public void Manifest_Icons_ContainSizeAndPurpose(string sizes, string purpose)
  {
    using var manifest = LoadManifest();

    var icons = manifest.RootElement.GetProperty("icons").EnumerateArray();
    Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == sizes && PurposeOf(i) == purpose);
  }

  [Fact]
  public void Manifest_Icons_AllFilesExistAndPngSizeMatchesDeclaredSize()
  {
    using var manifest = LoadManifest();

    var icons = manifest.RootElement.GetProperty("icons").EnumerateArray().ToList();
    Assert.NotEmpty(icons);
    foreach (var icon in icons)
    {
      var src = icon.GetProperty("src").GetString()!;
      var path = Path.Combine(WwwRoot, src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
      Assert.True(File.Exists(path), $"Icon '{src}' does not exist at '{path}'.");
      var (width, height) = ReadPngSize(path);
      Assert.Equal(icon.GetProperty("sizes").GetString(), $"{width}x{height}");
    }
  }

  [Theory]
  [InlineData("icon-192.png", 192)]
  [InlineData("icon-512.png", 512)]
  [InlineData("icon-512-maskable.png", 512)]
  public void IconFile_ExistsWithExpectedPixelSize(string fileName, int size)
  {
    var path = Path.Combine(WwwRoot, fileName);

    Assert.True(File.Exists(path), $"'{path}' does not exist.");
    Assert.Equal((size, size), ReadPngSize(path));
  }

  [Fact]
  public void ServiceWorker_Exists()
  {
    var path = Path.Combine(WwwRoot, "service-worker.js");

    Assert.True(File.Exists(path), $"'{path}' does not exist.");
  }

  private static JsonDocument LoadManifest()
  {
    var path = Path.Combine(WwwRoot, "manifest.webmanifest");
    Assert.True(File.Exists(path), $"'{path}' does not exist.");
    return JsonDocument.Parse(File.ReadAllText(path));
  }

  private static string PurposeOf(JsonElement icon)
  {
    return icon.TryGetProperty("purpose", out var purpose) ? purpose.GetString()! : "any";
  }

  private static (int Width, int Height) ReadPngSize(string path)
  {
    var bytes = File.ReadAllBytes(path);
    byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    Assert.True(bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(signature), $"'{path}' is not a PNG.");
    var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
    var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
    return (width, height);
  }
}
