using Karajob.Application.Files;

namespace Karajob.Infrastructure.Files;

public class LocalFileStorage(FileStorageOptions options) : IFileStorage
{
  public Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Stream OpenRead(string path)
  {
    throw new NotImplementedException();
  }

  public void Delete(string path)
  {
    throw new NotImplementedException();
  }
}
