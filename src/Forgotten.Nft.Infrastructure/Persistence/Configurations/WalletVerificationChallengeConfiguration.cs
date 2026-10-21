using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="WalletVerificationChallenge"/> (spec §9).</summary>
public sealed class WalletVerificationChallengeConfiguration : IEntityTypeConfiguration<WalletVerificationChallenge>
{
    public void Configure(EntityTypeBuilder<WalletVerificationChallenge> builder)
    {
        builder.ToTable("WalletVerificationChallenges");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.SessionTokenHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Nonce).IsRequired().HasMaxLength(64);
        builder.Property(x => x.WalletAddress).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => x.SessionTokenHash).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
