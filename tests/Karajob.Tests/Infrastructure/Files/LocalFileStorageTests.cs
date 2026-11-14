using Karajob.Application.Files;
using Karajob.Infrastructure.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Infrastructure.Files;

public class LocalFileStorageTests : IDisposable
{
  private readonly string root = Path.Combine(Path.GetTempPath(), "karajob-files-" + Guid.NewGuid().ToString("N"));
  private readonly LocalFileStorage storage;

  [Fact]
  public async Task SaveAsync_ThenOpenRead_RoundTripsBytes()
  {
    byte[] bytes = [1, 2, 3, 4, 5];

    await storage.SaveAsync("file.pdf", new MemoryStream(bytes));

    Assert.Equal(bytes, ReadAll("file.pdf"));
  }

  [Fact]
  public async Task SaveAsync_NestedPath_CreatesDirectoriesAndWritesUnderRoot()
  {
    await storage.SaveAsync("cv/abc/file.pdf", new MemoryStream([7, 8, 9]));

    Assert.True(File.Exists(Path.Combine(root, "cv", "abc", "file.pdf")));
  }

  [Fact]
  public async Task SaveAsync_ExistingPath_ThrowsIOExceptionAndKeepsOriginal()
  {
    await storage.SaveAsync("file.pdf", new MemoryStream([1, 1, 1]));

    await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync("file.pdf", new MemoryStream([2, 2, 2])));

    Assert.Equal<byte>([1, 1, 1], ReadAll("file.pdf"));
  }

  [Fact]
  public void OpenRead_Missing_ThrowsFileNotFound()
  {
    Assert.Throws<FileNotFoundException>(() => storage.OpenRead("cv/missing.pdf"));
  }

  [Fact]
  public async Task Delete_Existing_RemovesFile()
  {
    await storage.SaveAsync("cv/abc/file.pdf", new MemoryStream([1]));

    storage.Delete("cv/abc/file.pdf");

    Assert.False(File.Exists(Path.Combine(root, "cv", "abc", "file.pdf")));
    Assert.Throws<FileNotFoundException>(() => storage.OpenRead("cv/abc/file.pdf"));
  }

  [Fact]
  public void Delete_Missing_DoesNotThrow()
  {
    storage.Delete("cv/abc/missing.pdf");
  }

  [Theory]
  [InlineData("../x.pdf")]
  [InlineData("cv/../../x.pdf")]
  public async Task SaveAsync_PathEscapingRoot_ThrowsArgumentException(string path)
  {
    await Assert.ThrowsAnyAsync<ArgumentException>(() => storage.SaveAsync(path, new MemoryStream([1])));
  }

  [Fact]
  public async Task SaveAsync_AbsolutePath_ThrowsArgumentException()
  {
    var absolute = Path.Combine(Path.GetTempPath(), "karajob-outside-" + Guid.NewGuid().ToString("N") + ".pdf");

    await Assert.ThrowsAnyAsync<ArgumentException>(() => storage.SaveAsync(absolute, new MemoryStream([1])));

    Assert.False(File.Exists(absolute));
  }

  [Theory]
  [InlineData("../x.pdf")]
  [InlineData("cv/../../x.pdf")]
  public void OpenRead_PathEscapingRoot_ThrowsArgumentException(string path)
  {
    Assert.ThrowsAny<ArgumentException>(() => storage.OpenRead(path));
  }

  [Fact]
  public void OpenRead_AbsolutePath_ThrowsArgumentException()
  {
    Assert.ThrowsAny<ArgumentException>(() => storage.OpenRead(Path.Combine(Path.GetTempPath(), "x.pdf")));
  }

  [Theory]
  [InlineData("../x.pdf")]
  [InlineData("cv/../../x.pdf")]
  public void Delete_PathEscapingRoot_ThrowsArgumentException(string path)
  {
    Assert.ThrowsAny<ArgumentException>(() => storage.Delete(path));
  }

  [Fact]
  public void Delete_AbsolutePath_ThrowsArgumentException()
  {
    Assert.ThrowsAny<ArgumentException>(() => storage.Delete(Path.Combine(Path.GetTempPath(), "x.pdf")));
  }

  [Fact]
  public void AddKarajobFileStorage_RegistersLocalFileStorageAsSingleton()
  {
    var services = new ServiceCollection();

    services.AddKarajobFileStorage(new FileStorageOptions { RootDirectory = root });

    var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IFileStorage));
    Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    using var provider = services.BuildServiceProvider();
    Assert.IsType<LocalFileStorage>(provider.GetRequiredService<IFileStorage>());
    Assert.Same(provider.GetRequiredService<IFileStorage>(), provider.GetRequiredService<IFileStorage>());
  }

  public void Dispose()
  {
    if (Directory.Exists(root))
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private byte[] ReadAll(string path)
  {
    using var stream = storage.OpenRead(path);
    using var buffer = new MemoryStream();
    stream.CopyTo(buffer);
    return buffer.ToArray();
  }

  public LocalFileStorageTests()
  {
    storage = new LocalFileStorage(new FileStorageOptions { RootDirectory = root });
  }
}
