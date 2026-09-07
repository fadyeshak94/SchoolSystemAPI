using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStudentEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ClassRoomId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_ClassRooms_ClassRoomId",
                        column: x => x.ClassRoomId,
                        principalTable: "ClassRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_ClassRoomId",
                table: "StudentEnrollments",
                column: "ClassRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId_ClassRoomId_AcademicYear",
                table: "StudentEnrollments",
                columns: new[] { "StudentId", "ClassRoomId", "AcademicYear" },
                unique: true);

            // Migrate data
            migrationBuilder.Sql(@"
                INSERT INTO StudentEnrollments (StudentId, ClassRoomId, AcademicYear)
                SELECT Id, ClassRoomId, '2024/2025'
                FROM Students
                WHERE ClassRoomId IS NOT NULL AND ClassRoomId > 0;
            ");

            // Now drop old column
            migrationBuilder.DropForeignKey(
                name: "FK_Students_ClassRooms_ClassRoomId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ClassRoomId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ClassRoomId",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentEnrollments");

            migrationBuilder.AddColumn<int>(
                name: "ClassRoomId",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Restore data on down
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.ClassRoomId = e.ClassRoomId
                FROM Students s
                INNER JOIN (
                    SELECT StudentId, ClassRoomId
                    FROM StudentEnrollments
                    WHERE AcademicYear = '2024/2025'
                ) e ON s.Id = e.StudentId;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassRoomId",
                table: "Students",
                column: "ClassRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_ClassRooms_ClassRoomId",
                table: "Students",
                column: "ClassRoomId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
