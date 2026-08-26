using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingResetToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PendingReset",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingReset",
                table: "Users");
        }
    }
}
