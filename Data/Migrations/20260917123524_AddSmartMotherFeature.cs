using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartMotherFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotherId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mother",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Whatsapp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HusbandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfessionFather = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mother", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MotherAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MotherId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotherAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotherAttendance_Mother_MotherId",
                        column: x => x.MotherId,
                        principalTable: "Mother",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_MotherId",
                table: "Students",
                column: "MotherId");

            migrationBuilder.CreateIndex(
                name: "IX_MotherAttendance_MotherId",
                table: "MotherAttendance",
                column: "MotherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Mother_MotherId",
                table: "Students",
                column: "MotherId",
                principalTable: "Mother",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Mother_MotherId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "MotherAttendance");

            migrationBuilder.DropTable(
                name: "Mother");

            migrationBuilder.DropIndex(
                name: "IX_Students_MotherId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MotherId",
                table: "Students");
        }
    }
}
