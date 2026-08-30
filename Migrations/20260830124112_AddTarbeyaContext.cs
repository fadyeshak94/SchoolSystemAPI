using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTarbeyaContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TarbeyaClassId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TarbeyaFamilyId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TarbeyaFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaStages_TarbeyaFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "TarbeyaFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaClasses_TarbeyaStages_StageId",
                        column: x => x.StageId,
                        principalTable: "TarbeyaStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaStudents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GeneralNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaStudents_TarbeyaClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "TarbeyaClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaAttendances_TarbeyaStudents_StudentId",
                        column: x => x.StudentId,
                        principalTable: "TarbeyaStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TarbeyaClassId",
                table: "Users",
                column: "TarbeyaClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TarbeyaFamilyId",
                table: "Users",
                column: "TarbeyaFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaAttendances_StudentId_Date",
                table: "TarbeyaAttendances",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaClasses_StageId",
                table: "TarbeyaClasses",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaStages_FamilyId",
                table: "TarbeyaStages",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaStudents_ClassId",
                table: "TarbeyaStudents",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TarbeyaClasses_TarbeyaClassId",
                table: "Users",
                column: "TarbeyaClassId",
                principalTable: "TarbeyaClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TarbeyaFamilies_TarbeyaFamilyId",
                table: "Users",
                column: "TarbeyaFamilyId",
                principalTable: "TarbeyaFamilies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TarbeyaClasses_TarbeyaClassId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_TarbeyaFamilies_TarbeyaFamilyId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "TarbeyaAttendances");

            migrationBuilder.DropTable(
                name: "TarbeyaStudents");

            migrationBuilder.DropTable(
                name: "TarbeyaClasses");

            migrationBuilder.DropTable(
                name: "TarbeyaStages");

            migrationBuilder.DropTable(
                name: "TarbeyaFamilies");

            migrationBuilder.DropIndex(
                name: "IX_Users_TarbeyaClassId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TarbeyaFamilyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TarbeyaClassId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TarbeyaFamilyId",
                table: "Users");
        }
    }
}
