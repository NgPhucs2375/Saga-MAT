using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInventoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tồn kho đang tạm giữ (ReservedQty)
            migrationBuilder.AddColumn<int>(
                name: "ReservedQty",
                table: "Product",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Concurrency token (Optimistic Locking)
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Product",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "ReservedQty",
                table: "Product");
        }
    }
}