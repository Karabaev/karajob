namespace Karajob.Application.Transfer;

public class DatabaseTransferException(DatabaseTransferFailure failure, string message, Exception? inner = null)
  : Exception(message, inner)
{
  public DatabaseTransferFailure Failure { get; } = failure;
}
