using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildRoleMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Blockchain = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CollectionAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedByDiscordUserId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRoleMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NftOwnershipHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NftAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousOwnerAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    NewOwnerAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TransactionSignature = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftOwnershipHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NftOwnershipHistory_NftAssets_NftAssetId",
                        column: x => x.NftAssetId,
                        principalTable: "NftAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildRoleMappings_GuildId_Blockchain_CollectionAddress",
                table: "GuildRoleMappings",
                columns: new[] { "GuildId", "Blockchain", "CollectionAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NftOwnershipHistory_NftAssetId",
                table: "NftOwnershipHistory",
                column: "NftAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildRoleMappings");

            migrationBuilder.DropTable(
                name: "NftOwnershipHistory");
        }
    }
}
