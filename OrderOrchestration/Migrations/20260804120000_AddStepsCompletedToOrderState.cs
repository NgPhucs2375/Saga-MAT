using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderOrchestration.Migrations
{
    /// <inheritdoc />
    public partial class AddStepsCompletedToOrderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StepsCompleted",
                table: "OrderState",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StepsCompleted",
                table: "OrderState");
        }
    }
}