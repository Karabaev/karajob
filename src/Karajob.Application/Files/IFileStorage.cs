namespace Karajob.Application.Files;

// Paths are relative and '/'-separated, so they stay valid when the storage root moves.
public interface IFileStorage
{
  Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

  /// <exception cref="FileNotFoundException">No file at <paramref name="path"/>.</exception>
  Stream OpenRead(string path);

  /// <summary>Does nothing when the file is already gone.</summary>
  void Delete(string path);
}
