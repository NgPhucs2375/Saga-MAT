using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderOrchestration.Migrations
{
    /// <inheritdoc />
    public partial class Add_StepsCompleted_To_OrderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTimeout",
                table: "OrderState");

            migrationBuilder.DropColumn(
                name: "NeedReleaseInventory",
                table: "OrderState");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorReason",
                table: "OrderState",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ErrorReason",
                table: "OrderState",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsTimeout",
                table: "OrderState",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedReleaseInventory",
                table: "OrderState",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
