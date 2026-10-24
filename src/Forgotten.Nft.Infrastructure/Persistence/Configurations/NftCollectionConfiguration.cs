using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping for <see cref="NftCollection"/> (spec §9 "NftCollections", §13).
/// Discord IDs are stored as text to avoid SQLite signed-64-bit overflow. Approval
/// is unique per guild + blockchain + collection address.
/// </summary>
public sealed class NftCollectionConfiguration : IEntityTypeConfiguration<NftCollection>
{
    public void Configure(EntityTypeBuilder<NftCollection> builder)
    {
        builder.ToTable("NftCollections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GuildId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Blockchain).IsRequired().HasMaxLength(32);
        builder.Property(x => x.CollectionAddress).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DisplayName).HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.ApprovedByDiscordUserId).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.IsDefault).IsRequired();

        builder.HasIndex(x => new { x.GuildId, x.Blockchain, x.CollectionAddress }).IsUnique();
    }
}
