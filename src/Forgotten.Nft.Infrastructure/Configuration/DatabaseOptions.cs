using System.ComponentModel.DataAnnotations;

namespace Forgotten.Nft.Infrastructure.Configuration;

/// <summary>
/// SQLite database options. The database file is never auto-dropped or recreated;
/// schema changes flow exclusively through EF Core migrations (spec §9).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = "Data Source=forgotten.db";

    /// <summary>SQLite busy timeout in milliseconds, applied on each opened connection (spec §9.2).</summary>
    [Range(0, 60000)]
    public int BusyTimeoutMs { get; set; } = 5000;
}
