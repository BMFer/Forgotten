using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="NftAsset"/> (spec §9 "NftAssets", §9.2 rules).</summary>
public sealed class NftAssetConfiguration : IEntityTypeConfiguration<NftAsset>
{
    public void Configure(EntityTypeBuilder<NftAsset> builder)
    {
        builder.ToTable("NftAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Blockchain).IsRequired().HasMaxLength(32);
        builder.Property(x => x.AssetAddress).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CollectionAddress).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(256);
        builder.Property(x => x.Symbol).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(8192);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.AnimationUrl).HasMaxLength(2048);
        builder.Property(x => x.MetadataUri).HasMaxLength(2048);
        builder.Property(x => x.OwnerAddress).HasMaxLength(64);

        // Unique per blockchain asset identifier (spec §9.2).
        builder.HasIndex(x => new { x.Blockchain, x.AssetAddress }).IsUnique();
        builder.HasIndex(x => x.OwnerAddress);
        builder.HasIndex(x => x.CollectionAddress);

        builder.HasMany(x => x.Attributes)
            .WithOne(a => a.NftAsset!)
            .HasForeignKey(a => a.NftAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
