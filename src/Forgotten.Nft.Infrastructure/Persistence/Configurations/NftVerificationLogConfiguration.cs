using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="NftVerificationLog"/> (spec §9 "NftVerificationLogs").</summary>
public sealed class NftVerificationLogConfiguration : IEntityTypeConfiguration<NftVerificationLog>
{
    public void Configure(EntityTypeBuilder<NftVerificationLog> builder)
    {
        builder.ToTable("NftVerificationLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.GuildId).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AssetAddress).IsRequired().HasMaxLength(64);
        builder.Property(x => x.VerificationType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Provider).HasMaxLength(64);
        builder.Property(x => x.ErrorCode).HasMaxLength(64);

        builder.HasIndex(x => new { x.GuildId, x.CheckedAt });
        builder.HasIndex(x => x.AssetAddress);
    }
}
