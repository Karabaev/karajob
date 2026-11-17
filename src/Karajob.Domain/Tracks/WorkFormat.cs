namespace Karajob.Domain.Tracks;

[Flags]
public enum WorkFormat
{
  None = 0,
  Remote = 1,
  Hybrid = 2,
  OnSite = 4,
  Relocation = 8,
}
