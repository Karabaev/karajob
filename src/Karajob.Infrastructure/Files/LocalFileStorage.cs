using Karajob.Application.Files;

namespace Karajob.Infrastructure.Files;

public class LocalFileStorage(FileStorageOptions options) : IFileStorage
{
  public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
  {
    var fullPath = Resolve(path);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    await content.CopyToAsync(file, cancellationToken);
  }

  public Stream OpenRead(string path)
  {
    var fullPath = Resolve(path);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException("Stored file was not found.", path);
    }

    return File.OpenRead(fullPath);
  }

  public void Delete(string path)
  {
    var fullPath = Resolve(path);
    if (File.Exists(fullPath))
    {
      File.Delete(fullPath);
    }
  }

  private string Resolve(string path)
  {
    if (Path.IsPathRooted(path))
    {
      throw new ArgumentException("Storage path must be relative.", nameof(path));
    }

    var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RootDirectory));
    var fullPath = Path.GetFullPath(Path.Combine(root, path));
    if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    {
      throw new ArgumentException("Storage path escapes the storage root.", nameof(path));
    }

    return fullPath;
  }
}
