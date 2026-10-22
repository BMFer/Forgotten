using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the FORGOTTEN NFT engine.
///
/// Phase 0 intentionally declares no entities — it establishes the context,
/// SQLite configuration (WAL, busy timeout, foreign keys via
/// <see cref="ForgottenConnectionInterceptor"/>) and the UTC timestamp
/// convention. Phase 1 adds NftAssets / NftAttributes; later phases add the
/// remaining tables from spec §9. The database is only ever evolved through
/// EF Core migrations (spec §9 — never auto-dropped or recreated).
/// </summary>
public class ForgottenDbContext : DbContext
{
    public ForgottenDbContext(DbContextOptions<ForgottenDbContext> options)
        : base(options)
    {
    }

    public DbSet<NftAsset> NftAssets => Set<NftAsset>();

    public DbSet<NftAttribute> NftAttributes => Set<NftAttribute>();

    public DbSet<NftCollection> NftCollections => Set<NftCollection>();

    public DbSet<NftVerificationLog> NftVerificationLogs => Set<NftVerificationLog>();

    public DbSet<WalletVerificationChallenge> WalletVerificationChallenges => Set<WalletVerificationChallenge>();

    public DbSet<DiscordWalletConnection> DiscordWalletConnections => Set<DiscordWalletConnection>();

    public DbSet<NftGallerySession> NftGallerySessions => Set<NftGallerySession>();

    public DbSet<NftOwnershipHistory> NftOwnershipHistory => Set<NftOwnershipHistory>();

    public DbSet<GuildRoleMapping> GuildRoleMappings => Set<GuildRoleMapping>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Store every DateTime as UTC (spec §9.2). Inert until Phase 1 adds entities.
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForgottenDbContext).Assembly);
    }
}
