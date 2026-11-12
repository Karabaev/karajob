namespace Karajob.Domain.Vacancies;

public record SalaryRange
{
  public long? Min { get; }
  public long? Max { get; }
  public string Currency { get; }

  public SalaryRange(long? min, long? max, string currency)
  {
    if (min is null && max is null) throw new ArgumentException("At least one of min or max is required.");
    if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), "Salary must not be negative.");
    if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), "Salary must not be negative.");
    if (min > max) throw new ArgumentException("Min must not exceed max.", nameof(min));
    if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetter)) throw new ArgumentException("Currency must be a 3-letter code.", nameof(currency));

    Min = min;
    Max = max;
    Currency = currency.ToUpperInvariant();
  }
}
