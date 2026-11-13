using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class DataProtectionTests : IDisposable
{
  private const string KeysDirectorySetting = "DataProtection:KeysDirectory";

  private readonly string _keysDirectory = Path.Combine(Path.GetTempPath(), "karajob-dp-" + Guid.NewGuid().ToString("N"));

  [Fact]
  public void Protect_WithKeysDirectoryConfigured_WritesKeyFileToThatDirectory()
  {
    using var factory = CreateFactory();

    Protect(factory, "payload");

    Assert.NotEmpty(KeyFiles());
  }

  [Fact]
  public void Unprotect_AfterHostRecreatedWithSameKeysDirectory_ReturnsOriginalPayload()
  {
    string protectedPayload;
    using (var first = CreateFactory())
    {
      protectedPayload = Protect(first, "payload");
    }

    Assert.NotEmpty(KeyFiles());

    using var second = CreateFactory();
    var unprotected = second.Services.GetRequiredService<IDataProtectionProvider>()
      .CreateProtector("test")
      .Unprotect(protectedPayload);

    Assert.Equal("payload", unprotected);
  }

  public void Dispose()
  {
    if (Directory.Exists(_keysDirectory))
    {
      Directory.Delete(_keysDirectory, recursive: true);
    }
  }

  private static string Protect(WebApplicationFactory<Program> factory, string payload)
  {
    return factory.Services.GetRequiredService<IDataProtectionProvider>()
      .CreateProtector("test")
      .Protect(payload);
  }

  private string[] KeyFiles()
  {
    return Directory.Exists(_keysDirectory)
      ? Directory.GetFiles(_keysDirectory, "key-*.xml")
      : [];
  }

  private WebApplicationFactory<Program> CreateFactory()
  {
    return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
      builder.UseSetting(KeysDirectorySetting, _keysDirectory));
  }
}
