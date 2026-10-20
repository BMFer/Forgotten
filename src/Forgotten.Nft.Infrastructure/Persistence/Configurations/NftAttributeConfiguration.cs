using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="NftAttribute"/> (spec §9 "NftAttributes").</summary>
public sealed class NftAttributeConfiguration : IEntityTypeConfiguration<NftAttribute>
{
    public void Configure(EntityTypeBuilder<NftAttribute> builder)
    {
        builder.ToTable("NftAttributes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TraitType).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TraitValue).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.DisplayType).HasMaxLength(64);

        builder.HasIndex(x => x.NftAssetId);
    }
}
