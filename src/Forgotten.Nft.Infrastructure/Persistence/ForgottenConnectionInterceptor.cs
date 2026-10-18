using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>
/// Applies SQLite PRAGMAs on every opened connection (spec §9.2):
/// WAL journal mode, a busy timeout, and enforced foreign keys.
/// </summary>
public sealed class ForgottenConnectionInterceptor : DbConnectionInterceptor
{
    private readonly int _busyTimeoutMs;

    public ForgottenConnectionInterceptor(int busyTimeoutMs)
    {
        _busyTimeoutMs = busyTimeoutMs;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    private void Apply(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private string Pragmas =>
        $"PRAGMA journal_mode=WAL; PRAGMA busy_timeout={_busyTimeoutMs}; PRAGMA foreign_keys=ON;";
}
