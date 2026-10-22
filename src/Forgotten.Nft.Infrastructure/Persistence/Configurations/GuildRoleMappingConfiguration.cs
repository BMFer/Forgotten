using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forgotten.Nft.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="GuildRoleMapping"/> (spec §11). One role per collection per guild.</summary>
public sealed class GuildRoleMappingConfiguration : IEntityTypeConfiguration<GuildRoleMapping>
{
    public void Configure(EntityTypeBuilder<GuildRoleMapping> builder)
    {
        builder.ToTable("GuildRoleMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GuildId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Blockchain).IsRequired().HasMaxLength(32);
        builder.Property(x => x.CollectionAddress).IsRequired().HasMaxLength(64);
        builder.Property(x => x.RoleId).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.CreatedByDiscordUserId).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.GuildId, x.Blockchain, x.CollectionAddress }).IsUnique();
    }
}
