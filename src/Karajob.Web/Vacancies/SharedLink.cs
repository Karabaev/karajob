namespace Karajob.Web.Vacancies;

public record SharedLinkResult(string Url, string? Title, string? Note);

public static class SharedLink
{
  public static SharedLinkResult Extract(string? url, string? text, string? title)
  {
    throw new NotImplementedException();
  }
}
