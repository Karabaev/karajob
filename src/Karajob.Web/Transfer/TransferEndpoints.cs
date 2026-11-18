using Karajob.Application.Transfer;

namespace Karajob.Web.Transfer;

public static class TransferEndpoints
{
  public const string ExportPath = "/transfer/export";
  public const string ContentType = "application/zip";

  public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet(ExportPath, ExportAsync).RequireAuthorization();
    return endpoints;
  }

  private static async Task<IResult> ExportAsync(
    IDatabaseTransfer transfer,
    TimeProvider timeProvider,
    CancellationToken cancellationToken)
  {
    // The archive is built aside: a failure midway must produce an error status, not a truncated download.
    var file = new FileStream(
      Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
    try
    {
      await transfer.ExportAsync(file, cancellationToken);
      file.Position = 0;
    }
    catch
    {
      await file.DisposeAsync();
      throw;
    }

    var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
    return Results.File(file, ContentType, fileDownloadName: $"karajob-export-{stamp}.zip");
  }
}
