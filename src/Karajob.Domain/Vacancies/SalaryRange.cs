namespace Karajob.Domain.Vacancies;

public record SalaryRange
{
  public long? Min { get; }
  public long? Max { get; }
  public string Currency { get; }

  public SalaryRange(long? min, long? max, string currency) => throw new NotImplementedException();
}
