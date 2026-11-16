using Karajob.Application.Vacancies;
using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Vacancies;

public class HttpPageTitleReader(HttpClient http, ILogger<HttpPageTitleReader> logger) : IPageTitleReader
{
  public Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
