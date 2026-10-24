using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forgotten.Nft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NftCollections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NftCollections");
        }
    }
}
