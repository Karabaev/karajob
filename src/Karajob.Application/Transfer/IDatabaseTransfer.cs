namespace Karajob.Application.Transfer;

public interface IDatabaseTransfer
{
  /// <summary>Writes every profile's data, the shared reference data and the CV files as a ZIP archive.</summary>
  /// <exception cref="DatabaseTransferException">A stored CV file is missing.</exception>
  Task ExportAsync(Stream destination, CancellationToken cancellationToken = default);

  /// <summary>Replaces ALL existing data with the archive's content. Nothing is touched when the archive is invalid.</summary>
  /// <exception cref="DatabaseTransferException">The archive is invalid or from an incompatible version.</exception>
  Task<ImportSummary> ImportAsync(Stream source, CancellationToken cancellationToken = default);
}
