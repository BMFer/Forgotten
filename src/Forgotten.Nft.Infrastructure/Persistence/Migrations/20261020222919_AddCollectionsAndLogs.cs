using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionsAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NftCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Blockchain = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CollectionAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedByDiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NftVerificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    GuildId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AssetAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VerificationType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    VerificationStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftVerificationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NftCollections_GuildId_Blockchain_CollectionAddress",
                table: "NftCollections",
                columns: new[] { "GuildId", "Blockchain", "CollectionAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NftVerificationLogs_AssetAddress",
                table: "NftVerificationLogs",
                column: "AssetAddress");

            migrationBuilder.CreateIndex(
                name: "IX_NftVerificationLogs_GuildId_CheckedAt",
                table: "NftVerificationLogs",
                columns: new[] { "GuildId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NftCollections");

            migrationBuilder.DropTable(
                name: "NftVerificationLogs");
        }
    }
}
