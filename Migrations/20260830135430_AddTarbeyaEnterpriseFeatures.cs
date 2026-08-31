using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTarbeyaEnterpriseFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "TarbeyaStudents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "TarbeyaStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalNotes",
                table: "TarbeyaStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhone",
                table: "TarbeyaStudents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "TarbeyaStudents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TarbeyaPointTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaPointTransactions_TarbeyaStudents_StudentId",
                        column: x => x.StudentId,
                        principalTable: "TarbeyaStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TarbeyaPointTransactions_Users_ServantId",
                        column: x => x.ServantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaVisitationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaVisitationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaVisitationRecords_TarbeyaStudents_StudentId",
                        column: x => x.StudentId,
                        principalTable: "TarbeyaStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TarbeyaVisitationRecords_Users_ServantId",
                        column: x => x.ServantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaPointTransactions_ServantId",
                table: "TarbeyaPointTransactions",
                column: "ServantId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaPointTransactions_StudentId",
                table: "TarbeyaPointTransactions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaVisitationRecords_ServantId",
                table: "TarbeyaVisitationRecords",
                column: "ServantId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaVisitationRecords_StudentId",
                table: "TarbeyaVisitationRecords",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TarbeyaPointTransactions");

            migrationBuilder.DropTable(
                name: "TarbeyaVisitationRecords");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "TarbeyaStudents");

            migrationBuilder.DropColumn(
                name: "MedicalNotes",
                table: "TarbeyaStudents");

            migrationBuilder.DropColumn(
                name: "ParentPhone",
                table: "TarbeyaStudents");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "TarbeyaStudents");

            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "TarbeyaStudents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
