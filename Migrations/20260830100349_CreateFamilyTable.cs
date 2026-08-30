using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateFamilyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FamilyId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FatherPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MotherPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_FamilyId",
                table: "Students",
                column: "FamilyId");

            // Fix for existing invalid FamilyId data preventing FK constraint
            migrationBuilder.Sql("UPDATE Students SET FamilyId = NULL WHERE FamilyId IS NOT NULL AND FamilyId NOT IN (SELECT Id FROM Families)");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Families_FamilyId",
                table: "Students",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Families_FamilyId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropIndex(
                name: "IX_Students_FamilyId",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "FamilyId",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
