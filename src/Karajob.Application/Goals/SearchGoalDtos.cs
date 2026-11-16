namespace Karajob.Application.Goals;

public record SearchGoalInput(
  Guid? RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<Guid> PlatformIds,
  IReadOnlyList<Guid> EngineIds,
  string? Industries,
  string? MustHave,
  int WeeklyApplicationTarget,
  DateOnly? Deadline,
  long? DesiredMonthlyIncome,
  string? IncomeCurrency,
  int MarketThresholdPerWeek,
  int ConversionThresholdPermille,
  int MinSampleSize,
  int MaturationDays);

public record SearchGoalDetails(
  Guid? RoleId,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<Guid> PlatformIds,
  IReadOnlyList<Guid> EngineIds,
  string? Industries,
  string? MustHave,
  int WeeklyApplicationTarget,
  DateOnly? Deadline,
  long? DesiredMonthlyIncome,
  string? IncomeCurrency,
  int MarketThresholdPerWeek,
  int ConversionThresholdPermille,
  int MinSampleSize,
  int MaturationDays);
