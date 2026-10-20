using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Deliveries",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Deliveries");
        }
    }
}
