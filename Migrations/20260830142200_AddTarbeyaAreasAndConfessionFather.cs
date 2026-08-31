using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTarbeyaAreasAndConfessionFather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Area",
                table: "TarbeyaStudents",
                newName: "ConfessionFather");

            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "TarbeyaStudents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "TarbeyaStudents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TarbeyaAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaAreas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaStudents_AreaId",
                table: "TarbeyaStudents",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_TarbeyaStudents_TarbeyaAreas_AreaId",
                table: "TarbeyaStudents",
                column: "AreaId",
                principalTable: "TarbeyaAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TarbeyaStudents_TarbeyaAreas_AreaId",
                table: "TarbeyaStudents");

            migrationBuilder.DropTable(
                name: "TarbeyaAreas");

            migrationBuilder.DropIndex(
                name: "IX_TarbeyaStudents_AreaId",
                table: "TarbeyaStudents");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "TarbeyaStudents");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "TarbeyaStudents");

            migrationBuilder.RenameColumn(
                name: "ConfessionFather",
                table: "TarbeyaStudents",
                newName: "Area");
        }
    }
}
