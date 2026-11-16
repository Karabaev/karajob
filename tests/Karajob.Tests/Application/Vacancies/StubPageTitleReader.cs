using Karajob.Application.Vacancies;

namespace Karajob.Tests.Application.Vacancies;

internal class StubPageTitleReader : IPageTitleReader
{
  public string? Title { get; set; }
  public List<string> RequestedUrls { get; } = [];

  public Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken = default)
  {
    RequestedUrls.Add(url);
    return Task.FromResult(Title);
  }
}
