using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Karajob.Infrastructure.Persistence;

// SQLite returns DateTime with Kind=Unspecified, and domain constructors reject non-UTC values.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
  v => v,
  v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
