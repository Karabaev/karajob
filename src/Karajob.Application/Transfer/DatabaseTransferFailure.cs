namespace Karajob.Application.Transfer;

public enum DatabaseTransferFailure
{
  NotAnArchive = 0,
  DataFileMissing = 1,
  UnsupportedFormatVersion = 2,
  SchemaMismatch = 3,
  InvalidData = 4,
  CvFileMissing = 5
}
