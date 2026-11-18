using Karajob.Application.Transfer;

namespace Karajob.Tests.Web.Components.Settings;

public class StubDatabaseTransfer : IDatabaseTransfer
{
  public Exception? ImportFailure { get; set; }
  public int ImportCalls { get; private set; }

  public Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }

  public Task<ImportSummary> ImportAsync(Stream source, CancellationToken cancellationToken = default)
  {
    ImportCalls++;
    if (ImportFailure is not null)
    {
      throw ImportFailure;
    }

    return Task.FromResult(new ImportSummary(new Dictionary<string, int>(), 0));
  }
}
