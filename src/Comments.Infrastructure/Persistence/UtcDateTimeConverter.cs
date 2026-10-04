using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Comments.Infrastructure.Persistence;

public sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));