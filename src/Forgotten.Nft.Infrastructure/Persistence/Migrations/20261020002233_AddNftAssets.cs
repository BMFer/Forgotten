using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNftAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NftAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Blockchain = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AssetAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CollectionAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AnimationUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    MetadataUri = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    OwnerAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsCompressed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBurned = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastFetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NftAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NftAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    TraitType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TraitValue = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    DisplayType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NftAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NftAttributes_NftAssets_NftAssetId",
                        column: x => x.NftAssetId,
                        principalTable: "NftAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NftAssets_Blockchain_AssetAddress",
                table: "NftAssets",
                columns: new[] { "Blockchain", "AssetAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NftAssets_CollectionAddress",
                table: "NftAssets",
                column: "CollectionAddress");

            migrationBuilder.CreateIndex(
                name: "IX_NftAssets_OwnerAddress",
                table: "NftAssets",
                column: "OwnerAddress");

            migrationBuilder.CreateIndex(
                name: "IX_NftAttributes_NftAssetId",
                table: "NftAttributes",
                column: "NftAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NftAttributes");

            migrationBuilder.DropTable(
                name: "NftAssets");
        }
    }
}
