namespace Karajob.Application.Vacancies;

public interface IPageTitleReader
{
  // null when the page cannot be read or has no usable title: an unreadable page is an expected outcome, not an error.
  Task<string?> ReadTitleAsync(string url, CancellationToken cancellationToken = default);
}
