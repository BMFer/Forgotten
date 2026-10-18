using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>
/// Ensures all <see cref="DateTime"/> values are stored as UTC and read back with
/// <see cref="DateTimeKind.Utc"/> (spec §9.2: "Use UTC timestamps."). Registered
/// as a model-wide convention so Phase 1+ entities inherit it automatically.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
