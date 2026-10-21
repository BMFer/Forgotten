using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="NftGallerySession"/> (spec §9 "NftGallerySessions").</summary>
public sealed class NftGallerySessionConfiguration : IEntityTypeConfiguration<NftGallerySession>
{
    public void Configure(EntityTypeBuilder<NftGallerySession> builder)
    {
        builder.ToTable("NftGallerySessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.GuildId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.WalletAddress).HasMaxLength(64);
        builder.Property(x => x.CollectionFilter).HasMaxLength(64);

        builder.HasIndex(x => x.DiscordUserId);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
