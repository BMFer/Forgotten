using Forgotten.Nft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.UnitTests.Persistence;

/// <summary>
/// Proves the EF Core migration pipeline works end-to-end against SQLite: a fresh
/// database is created by applying migrations (never hand-made) — Phase 0 Exit Gate
/// and spec §9.
/// </summary>
public class DatabaseMigrationTests : IDisposable
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"forgotten-test-{Guid.NewGuid():N}.db");

    private DbContextOptions<ForgottenDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<ForgottenDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

    [Fact]
    public void Migrate_CreatesFreshSqliteDatabase()
    {
        Assert.False(File.Exists(_databasePath));

        using (var context = new ForgottenDbContext(CreateOptions()))
        {
            context.Database.Migrate();
        }

        Assert.True(File.Exists(_databasePath));
    }

    [Fact]
    public void Migrate_RecordsAppliedMigrations()
    {
        using var context = new ForgottenDbContext(CreateOptions());
        context.Database.Migrate();

        var applied = context.Database.GetAppliedMigrations().ToList();

        Assert.Contains(applied, m => m.Contains("InitialCreate", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        // SQLite keeps pooled handles; clear them so the temp file can be deleted.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
