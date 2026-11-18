namespace Karajob.Application.Transfer;

public record ImportSummary(IReadOnlyDictionary<string, int> RowsByTable, int FileCount)
{
  public int TotalRows => RowsByTable.Values.Sum();
}
