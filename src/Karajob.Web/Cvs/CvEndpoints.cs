using Karajob.Application.Cvs;

namespace Karajob.Web.Cvs;

public static class CvEndpoints
{
  public static IEndpointRouteBuilder MapCvEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/cvs/versions/{versionId:guid}/file", DownloadVersionAsync);
    return endpoints;
  }

  private static async Task<IResult> DownloadVersionAsync(Guid versionId, CvService cvService, CancellationToken cancellationToken)
  {
    try
    {
      var file = await cvService.OpenVersionAsync(versionId, cancellationToken);
      return file is null
        ? Results.NotFound()
        : Results.File(file.Content, file.ContentType, fileDownloadName: file.FileName);
    }
    catch (FileNotFoundException)
    {
      return Results.NotFound();
    }
  }
}
