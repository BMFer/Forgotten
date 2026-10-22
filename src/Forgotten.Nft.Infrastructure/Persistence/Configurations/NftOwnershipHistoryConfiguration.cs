using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="NftOwnershipHistory"/> (spec §9).</summary>
public sealed class NftOwnershipHistoryConfiguration : IEntityTypeConfiguration<NftOwnershipHistory>
{
    public void Configure(EntityTypeBuilder<NftOwnershipHistory> builder)
    {
        builder.ToTable("NftOwnershipHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PreviousOwnerAddress).HasMaxLength(64);
        builder.Property(x => x.NewOwnerAddress).HasMaxLength(64);
        builder.Property(x => x.Source).IsRequired().HasMaxLength(32);
        builder.Property(x => x.TransactionSignature).HasMaxLength(128);

        builder.HasIndex(x => x.NftAssetId);

        builder.HasOne(x => x.NftAsset)
            .WithMany()
            .HasForeignKey(x => x.NftAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
