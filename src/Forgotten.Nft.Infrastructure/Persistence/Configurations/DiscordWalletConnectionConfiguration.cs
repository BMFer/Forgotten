using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping for <see cref="DiscordWalletConnection"/> (spec §9/§9.2, §7.4).
/// A partial unique index enforces that a wallet is actively linked to at most one
/// identity at a time (duplicate-claim prevention), and a per-user+wallet unique
/// index keeps a single row per link (reactivated on reconnect).
/// </summary>
public sealed class DiscordWalletConnectionConfiguration : IEntityTypeConfiguration<DiscordWalletConnection>
{
    public void Configure(EntityTypeBuilder<DiscordWalletConnection> builder)
    {
        builder.ToTable("DiscordWalletConnections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.WalletAddress).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Blockchain).IsRequired().HasMaxLength(32);

        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => x.DiscordUserId);
        builder.HasIndex(x => new { x.DiscordUserId, x.Blockchain, x.WalletAddress }).IsUnique();

        // Only one ACTIVE owner per wallet (spec §9.2, §7.4 duplicate-claim prevention).
        builder.HasIndex(x => new { x.Blockchain, x.WalletAddress })
            .IsUnique()
            .HasFilter("\"DisconnectedAt\" IS NULL");
    }
}
