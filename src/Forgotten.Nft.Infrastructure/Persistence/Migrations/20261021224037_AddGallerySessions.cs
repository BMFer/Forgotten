using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGallerySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NftGallerySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GuildId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WalletAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CollectionFilter = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CurrentPage = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftGallerySessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NftGallerySessions_DiscordUserId",
                table: "NftGallerySessions",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NftGallerySessions_ExpiresAt",
                table: "NftGallerySessions",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NftGallerySessions");
        }
    }
}
