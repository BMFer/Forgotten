using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordWalletConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WalletAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Blockchain = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordWalletConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletVerificationChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SessionTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Nonce = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WalletAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletVerificationChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordWalletConnections_Blockchain_WalletAddress",
                table: "DiscordWalletConnections",
                columns: new[] { "Blockchain", "WalletAddress" },
                unique: true,
                filter: "\"DisconnectedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordWalletConnections_DiscordUserId",
                table: "DiscordWalletConnections",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordWalletConnections_DiscordUserId_Blockchain_WalletAddress",
                table: "DiscordWalletConnections",
                columns: new[] { "DiscordUserId", "Blockchain", "WalletAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletVerificationChallenges_ExpiresAt",
                table: "WalletVerificationChallenges",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletVerificationChallenges_SessionTokenHash",
                table: "WalletVerificationChallenges",
                column: "SessionTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordWalletConnections");

            migrationBuilder.DropTable(
                name: "WalletVerificationChallenges");
        }
    }
}
