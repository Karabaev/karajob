using System.Net.Mime;
using Karajob.Application.Cvs;

namespace Karajob.Web.Cvs;

public static class CvEndpoints
{
  private const string PdfContentType = "application/pdf";

  public static IEndpointRouteBuilder MapCvEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/cvs/versions/{versionId:guid}/file", ServeVersionAsync).RequireAuthorization();
    return endpoints;
  }

  private static async Task<IResult> ServeVersionAsync(
    Guid versionId,
    bool? inline,
    HttpContext httpContext,
    CvService cvService,
    CancellationToken cancellationToken)
  {
    try
    {
      var file = await cvService.OpenVersionAsync(versionId, cancellationToken);
      if (file is null)
      {
        return Results.NotFound();
      }

      if (inline != true || file.ContentType != PdfContentType)
      {
        return Results.File(file.Content, file.ContentType, fileDownloadName: file.FileName);
      }

      var disposition = new ContentDisposition { Inline = true, FileName = file.FileName };
      httpContext.Response.Headers.ContentDisposition = disposition.ToString();
      httpContext.Response.Headers.XContentTypeOptions = "nosniff";
      return Results.File(file.Content, file.ContentType, enableRangeProcessing: true);
    }
    catch (FileNotFoundException)
    {
      return Results.NotFound();
    }
  }
}
