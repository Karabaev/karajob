using Karajob.Application.Files;

namespace Karajob.Tests.Application.Cvs;

internal class InMemoryFileStorage : IFileStorage
{
  private readonly Dictionary<string, byte[]> files = [];

  public IReadOnlyCollection<string> Paths => files.Keys.ToList();

  public byte[] ReadAll(string path)
  {
    return files[path];
  }

  public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
  {
    using var buffer = new MemoryStream();
    await content.CopyToAsync(buffer, cancellationToken);
    files.Add(path, buffer.ToArray());
  }

  public Stream OpenRead(string path)
  {
    if (!files.TryGetValue(path, out var bytes))
    {
      throw new FileNotFoundException(path);
    }

    return new MemoryStream(bytes);
  }

  public void Delete(string path)
  {
    files.Remove(path);
  }
}
