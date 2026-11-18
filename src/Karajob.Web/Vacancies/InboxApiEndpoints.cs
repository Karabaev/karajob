using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Web.Resources;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

namespace Karajob.Web.Vacancies;

public static class InboxApiEndpoints
{
  public const string InboxPath = "/api/inbox";

  public static IEndpointRouteBuilder MapInboxApiEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapPost(InboxPath, AddAsync)
      .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = ApiTokenAuthenticationHandler.Scheme })
      .DisableAntiforgery()
      .AddEndpointFilter(SetCurrentUserAsync);
    return endpoints;
  }

  private static ValueTask<object?> SetCurrentUserAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
  {
    var httpContext = context.HttpContext;
    httpContext.RequestServices.GetRequiredService<ProfileCurrentUser>().SetUser(httpContext.User);
    return next(context);
  }

  private static async Task<IResult> AddAsync(
    InboxRequest request,
    VacancyService vacancyService,
    IStringLocalizer<VacancyStrings> localizer,
    CancellationToken cancellationToken)
  {
    try
    {
      var result = await vacancyService.QuickAddAsync(new QuickAddInput(request.Url ?? string.Empty, request.Note), cancellationToken);
      var message = localizer[result.AlreadyExisted ? "Api.Inbox.AlreadyExists" : "Api.Inbox.Added"].Value;
      var body = new InboxResponse(result.VacancyId, result.AlreadyExisted, result.Stage.ToString(), message);
      return result.AlreadyExisted
        ? Results.Ok(body)
        : Results.Json(body, statusCode: StatusCodes.Status201Created);
    }
    catch (ValidationFailedException ex)
    {
      return Results.Json(new { errors = ex.Errors }, statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private record InboxRequest(string? Url, string? Note);

  private record InboxResponse(Guid VacancyId, bool AlreadyExisted, string Stage, string Message);
}
